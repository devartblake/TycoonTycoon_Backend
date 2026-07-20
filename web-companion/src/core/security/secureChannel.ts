/**
 * KMS secure-channel client (syn-sec-v1).
 *
 * Mirrors the server protocol implemented by:
 * - Synaptix.Security.Kms.Application/Sessions/SecureSessionService.cs (handshake)
 * - Synaptix.Security.Kms.Application/Payload/SecurePayloadService.cs (envelopes)
 * - Synaptix.Backend.Api/Security/SecureChannelFilter.cs (headers + AAD)
 *
 * Handshake: POST /security/sessions/start with an ECDH public key (SPKI).
 *   sharedSecret = ECDH raw agreement (X25519 or P-256)
 *   salt         = SHA-256(clientNonce ‖ serverNonce ‖ sessionId as .NET Guid bytes)
 *   c2sKey       = HKDF-SHA256(sharedSecret, 32, salt, "synaptix:c2s:v1")
 *   s2cKey       = HKDF-SHA256(sharedSecret, 32, salt, "synaptix:s2c:v1")
 *   serverSignature = HMAC-SHA256(s2cKey,
 *     "{sessionId:N}:{serverPublicKey}:{expiresAt:O}:{suite}:{advertisedSuites|joined}")
 *
 * Envelope: AES-256-GCM, 12-byte nonce, 16-byte tag (sent as `mac`), fields
 * base64url-unpadded. AAD string:
 *   "syn-sec-v1|{request|response}|{METHOD}|{target}|{sessionId:N}|{seq}|{subjectId}"
 * Headers: X-Syn-Sec-Session, X-Syn-Sec-Seq (positive, unique per session),
 * X-Syn-Sec-Nonce (random replay nonce).
 */

export const SUITE_X25519 = 'X25519-HKDF-SHA256-AES256GCM';
export const SUITE_P256 = 'P256-HKDF-SHA256-AES256GCM';
const PROTOCOL = 'syn-sec-v1';
const EXPIRY_MARGIN_MS = 60_000;

export interface EncryptedEnvelope {
  ciphertext: string;
  nonce: string;
  mac: string;
  contentType: string;
  encryptedAtUtc: string;
}

export interface HandshakeResponse {
  sessionId: string;
  protocolVersion: string;
  selectedSuite: string;
  serverPublicKey: string;
  serverNonce: string;
  expiresAtUtc: string;
  serverSignature: string;
}

export interface SecureChannelDeps {
  /** Plain-JSON POST through the normal API client (bearer token attached). */
  post: (url: string, data: unknown) => Promise<{ data: any }>;
  getDeviceId: () => string;
  /** JWT "sub" of the authenticated user; part of the AAD. */
  getSubjectId: () => string;
  /** The crypto provider — window.crypto in the browser, node:crypto webcrypto in tests. */
  crypto?: Crypto;
  /** Force a specific suite instead of feature-detecting (tests / compatibility escape hatch). */
  forceSuite?: typeof SUITE_X25519 | typeof SUITE_P256;
}

// --- Encoding helpers (must match the .NET side exactly) ---

export function b64urlEncode(bytes: Uint8Array): string {
  let bin = '';
  for (const b of bytes) bin += String.fromCharCode(b);
  return btoa(bin).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

export function b64urlDecode(input: string): Uint8Array {
  const padded = input.replace(/-/g, '+').replace(/_/g, '/');
  const bin = atob(padded + '='.repeat((4 - (padded.length % 4)) % 4));
  const out = new Uint8Array(bin.length);
  for (let i = 0; i < bin.length; i++) out[i] = bin.charCodeAt(i);
  return out;
}

/** .NET Guid.ToByteArray(): first three groups little-endian, rest as-is. */
export function guidToDotNetBytes(guid: string): Uint8Array {
  const hex = guid.replace(/-/g, '').toLowerCase();
  if (hex.length !== 32) throw new Error(`Invalid GUID: ${guid}`);
  const b = new Uint8Array(16);
  for (let i = 0; i < 16; i++) b[i] = parseInt(hex.slice(i * 2, i * 2 + 2), 16);
  return new Uint8Array([
    b[3], b[2], b[1], b[0],
    b[5], b[4],
    b[7], b[6],
    b[8], b[9], b[10], b[11], b[12], b[13], b[14], b[15],
  ]);
}

/**
 * Reconstruct .NET's round-trip ("O") format from a System.Text.Json
 * DateTimeOffset string: fraction padded to exactly 7 digits, offset kept
 * as ±HH:MM ("Z" normalized to "+00:00"). String-level — never goes through
 * Date, which would lose sub-millisecond digits.
 */
export function toDotNetRoundTrip(iso: string): string {
  const m = iso.match(/^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.(\d+))?(Z|[+-]\d{2}:\d{2})$/);
  if (!m) throw new Error(`Unrecognized timestamp: ${iso}`);
  const frac = (m[2] ?? '').padEnd(7, '0').slice(0, 7);
  const offset = m[3] === 'Z' ? '+00:00' : m[3];
  return `${m[1]}.${frac}${offset}`;
}

function utf8(s: string): Uint8Array {
  return new TextEncoder().encode(s);
}

function concatBytes(...parts: Uint8Array[]): Uint8Array {
  const out = new Uint8Array(parts.reduce((n, p) => n + p.length, 0));
  let o = 0;
  for (const p of parts) {
    out.set(p, o);
    o += p.length;
  }
  return out;
}

export function buildAad(
  direction: 'request' | 'response',
  method: string,
  target: string,
  sessionIdN: string,
  seq: number,
  subjectId: string
): string {
  return [PROTOCOL, direction, method.toUpperCase(), target, sessionIdN, String(seq), subjectId].join('|');
}

interface ActiveSession {
  id: string;
  idN: string;
  c2sKey: CryptoKey;
  s2cKey: CryptoKey;
  expiresAtMs: number;
  seq: number;
  subjectId: string;
}

export class SecureChannel {
  private deps: SecureChannelDeps;
  private session: ActiveSession | null = null;
  private starting: Promise<ActiveSession> | null = null;

  constructor(deps: SecureChannelDeps) {
    this.deps = deps;
  }

  private get crypto(): Crypto {
    return this.deps.crypto ?? globalThis.crypto;
  }

  invalidate() {
    this.session = null;
  }

  private async supportsX25519(): Promise<boolean> {
    try {
      await this.crypto.subtle.generateKey({ name: 'X25519' } as Algorithm, false, ['deriveBits']);
      return true;
    } catch {
      return false;
    }
  }

  private async ensureSession(): Promise<ActiveSession> {
    if (this.session && Date.now() < this.session.expiresAtMs - EXPIRY_MARGIN_MS) {
      return this.session;
    }
    // Single-flight: concurrent secure calls share one handshake
    if (!this.starting) {
      this.starting = this.handshake().finally(() => {
        this.starting = null;
      });
    }
    return this.starting;
  }

  private async handshake(): Promise<ActiveSession> {
    const subtle = this.crypto.subtle;
    const useX25519 = this.deps.forceSuite
      ? this.deps.forceSuite === SUITE_X25519
      : await this.supportsX25519();

    // The handshake carries ONE clientPublicKey, so we advertise exactly the
    // suite that key serves (an X25519 SPKI can't complete a P-256 exchange).
    const preferredSuite = useX25519 ? SUITE_X25519 : SUITE_P256;
    const advertised = [preferredSuite];
    const keyGenAlg = useX25519
      ? ({ name: 'X25519' } as Algorithm)
      : ({ name: 'ECDH', namedCurve: 'P-256' } as EcKeyGenParams);
    const keyPair = (await subtle.generateKey(keyGenAlg, false, ['deriveBits'])) as CryptoKeyPair;

    const clientNonce = this.crypto.getRandomValues(new Uint8Array(24));
    const clientPublicKey = new Uint8Array(await subtle.exportKey('spki', keyPair.publicKey));

    const response = await this.deps.post('/security/sessions/start', {
      deviceId: this.deps.getDeviceId(),
      clientNonce: b64urlEncode(clientNonce),
      clientPublicKey: b64urlEncode(clientPublicKey),
      supportedSuites: advertised,
    });
    const hs = response.data as HandshakeResponse;

    if (hs.selectedSuite !== preferredSuite) {
      throw new Error(`Server selected unsupported suite: ${hs.selectedSuite}`);
    }

    // ECDH raw shared secret (matches .NET DeriveRawSecretAgreement)
    const serverPubBytes = b64urlDecode(hs.serverPublicKey);
    const importAlg =
      preferredSuite === SUITE_X25519
        ? ({ name: 'X25519' } as Algorithm)
        : ({ name: 'ECDH', namedCurve: 'P-256' } as EcKeyImportParams);
    const serverPublic = await subtle.importKey('spki', serverPubBytes as BufferSource, importAlg, false, []);
    const deriveAlg =
      preferredSuite === SUITE_X25519
        ? ({ name: 'X25519', public: serverPublic } as unknown as AlgorithmIdentifier)
        : ({ name: 'ECDH', public: serverPublic } as unknown as AlgorithmIdentifier);
    const sharedSecret = new Uint8Array(await subtle.deriveBits(deriveAlg, keyPair.privateKey, 256));

    // salt = SHA-256(clientNonce ‖ serverNonce ‖ Guid.ToByteArray(sessionId))
    const salt = new Uint8Array(
      await subtle.digest(
        'SHA-256',
        concatBytes(clientNonce, b64urlDecode(hs.serverNonce), guidToDotNetBytes(hs.sessionId)) as BufferSource
      )
    );

    const ikm = await subtle.importKey('raw', sharedSecret as BufferSource, 'HKDF', false, ['deriveBits']);
    const hkdf = (info: string) =>
      subtle.deriveBits(
        { name: 'HKDF', hash: 'SHA-256', salt: salt as BufferSource, info: utf8(info) as BufferSource },
        ikm,
        256
      );
    const c2sBits = new Uint8Array(await hkdf('synaptix:c2s:v1'));
    const s2cBits = new Uint8Array(await hkdf('synaptix:s2c:v1'));

    // Verify the negotiation transcript signature (downgrade detection):
    // HMAC-SHA256(s2cKey, "{id:N}:{serverPub}:{expires:O}:{suite}:{advertised|}")
    const idN = hs.sessionId.replace(/-/g, '').toLowerCase();
    const sigInput = utf8(
      `${idN}:${hs.serverPublicKey}:${toDotNetRoundTrip(hs.expiresAtUtc)}:${hs.selectedSuite}:${advertised.join('|')}`
    );
    const hmacKey = await subtle.importKey(
      'raw',
      s2cBits as BufferSource,
      { name: 'HMAC', hash: 'SHA-256' },
      false,
      ['verify']
    );
    const sigOk = await subtle.verify(
      'HMAC',
      hmacKey,
      b64urlDecode(hs.serverSignature) as BufferSource,
      sigInput as BufferSource
    );
    if (!sigOk) {
      throw new Error('Secure session signature verification failed (possible downgrade attack).');
    }

    const importAes = (bits: Uint8Array, usage: KeyUsage[]) =>
      subtle.importKey('raw', bits as BufferSource, { name: 'AES-GCM' }, false, usage);

    this.session = {
      id: hs.sessionId,
      idN,
      c2sKey: await importAes(c2sBits, ['encrypt']),
      s2cKey: await importAes(s2cBits, ['decrypt']),
      expiresAtMs: Date.parse(hs.expiresAtUtc),
      seq: 0,
      subjectId: this.deps.getSubjectId(),
    };
    return this.session;
  }

  /** Encrypt a request body; returns the envelope, headers, and the sequence used. */
  async encryptRequest(
    method: string,
    target: string,
    body: unknown
  ): Promise<{ envelope: EncryptedEnvelope; headers: Record<string, string>; seq: number }> {
    const session = await this.ensureSession();
    const seq = ++session.seq;
    const replayNonce = b64urlEncode(this.crypto.getRandomValues(new Uint8Array(16)));

    const aad = buildAad('request', method, target, session.idN, seq, session.subjectId);
    const nonce = this.crypto.getRandomValues(new Uint8Array(12));
    const plaintext = utf8(JSON.stringify(body ?? {}));

    const sealed = new Uint8Array(
      await this.crypto.subtle.encrypt(
        {
          name: 'AES-GCM',
          iv: nonce as BufferSource,
          additionalData: utf8(aad) as BufferSource,
          tagLength: 128,
        },
        session.c2sKey,
        plaintext as BufferSource
      )
    );
    // WebCrypto returns ciphertext ‖ tag; the wire format splits them
    const ciphertext = sealed.slice(0, sealed.length - 16);
    const tag = sealed.slice(sealed.length - 16);

    return {
      envelope: {
        ciphertext: b64urlEncode(ciphertext),
        nonce: b64urlEncode(nonce),
        mac: b64urlEncode(tag),
        contentType: 'application/json',
        encryptedAtUtc: new Date().toISOString(),
      },
      headers: {
        'X-Syn-Sec-Session': session.id,
        'X-Syn-Sec-Seq': String(seq),
        'X-Syn-Sec-Nonce': replayNonce,
      },
      seq,
    };
  }

  /** Decrypt a response envelope produced for the request with sequence `seq`. */
  async decryptResponse(
    method: string,
    target: string,
    seq: number,
    envelope: EncryptedEnvelope
  ): Promise<any> {
    const session = this.session;
    if (!session) throw new Error('No active secure session.');

    const aad = buildAad('response', method, target, session.idN, seq, session.subjectId);
    const ciphertext = b64urlDecode(envelope.ciphertext);
    const tag = b64urlDecode(envelope.mac);

    const plaintext = new Uint8Array(
      await this.crypto.subtle.decrypt(
        {
          name: 'AES-GCM',
          iv: b64urlDecode(envelope.nonce) as BufferSource,
          additionalData: utf8(aad) as BufferSource,
          tagLength: 128,
        },
        session.s2cKey,
        concatBytes(ciphertext, tag) as BufferSource
      )
    );

    const text = new TextDecoder().decode(plaintext);
    if (!text) return null;
    try {
      return JSON.parse(text);
    } catch {
      return text;
    }
  }
}

export function isEncryptedEnvelope(data: unknown): data is EncryptedEnvelope {
  return (
    typeof data === 'object' &&
    data !== null &&
    typeof (data as any).ciphertext === 'string' &&
    typeof (data as any).nonce === 'string' &&
    typeof (data as any).mac === 'string'
  );
}
