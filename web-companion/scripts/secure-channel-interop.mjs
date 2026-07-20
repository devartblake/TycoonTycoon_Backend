/**
 * Interop test for the KMS secure-channel client (src/core/security/secureChannel.ts).
 *
 * Plays the SERVER role with an independent implementation of the protocol,
 * written directly from the C# sources (SecureSessionService.cs,
 * SecurePayloadService.cs, SecureChannelFilter.cs) using node:crypto primitives
 * (hkdfSync, createCipheriv aes-256-gcm, createHmac) — deliberately NOT the
 * WebCrypto calls the client uses, so encoding/derivation asymmetries fail loudly.
 *
 * Run: npm run test:secure-channel
 * (esbuild transpiles the TS module to node_modules/.cache first)
 *
 * This proves protocol self-consistency against an independent reading of the
 * C# code. Final sign-off still needs one run against a real KMS in staging.
 */

import {
  webcrypto,
  createCipheriv,
  createDecipheriv,
  createHash,
  createHmac,
  hkdfSync,
  randomBytes,
  randomUUID,
} from 'node:crypto';
import assert from 'node:assert/strict';
import {
  SecureChannel,
  SUITE_X25519,
  SUITE_P256,
  guidToDotNetBytes,
  toDotNetRoundTrip,
} from '../node_modules/.cache/secure-channel/secureChannel.mjs';

const SUBJECT = 'f47ac10b-58cc-4372-a567-0e02b2c3d479';
const DEVICE = 'web_test-device';

const b64url = (buf) => Buffer.from(buf).toString('base64url');

/** .NET Guid.ToByteArray(), implemented independently for the server role. */
function dotnetGuidBytes(guid) {
  const h = guid.replace(/-/g, '');
  const b = Buffer.from(h, 'hex');
  return Buffer.from([
    b[3], b[2], b[1], b[0], b[5], b[4], b[7], b[6],
    b[8], b[9], b[10], b[11], b[12], b[13], b[14], b[15],
  ]);
}

/** System.Text.Json DateTimeOffset serialization: trim trailing fraction zeros, offset +00:00. */
function stjDateTimeOffset(date) {
  const iso = date.toISOString(); // YYYY-MM-DDTHH:mm:ss.sssZ
  let [main, rest] = [iso.slice(0, 19), iso.slice(19, -1)]; // rest = ".sss" or ""
  let frac = rest.replace(/^\./, '').replace(/0+$/, '');
  return frac ? `${main}.${frac}+00:00` : `${main}+00:00`;
}

/** .NET "O" (round-trip) format: always 7 fraction digits. */
function dotnetRoundTrip(date) {
  const iso = date.toISOString();
  const frac = iso.slice(20, -1).padEnd(7, '0');
  return `${iso.slice(0, 19)}.${frac}+00:00`;
}

function makeServer(suite, { forceZeroMs = false, tamperSignature = false } = {}) {
  const state = {};

  return {
    state,
    async handlePost(url, body) {
      assert.equal(url, '/security/sessions/start');
      assert.equal(body.deviceId, DEVICE);
      assert.deepEqual(body.supportedSuites, [suite]);

      const alg =
        suite === SUITE_X25519 ? { name: 'X25519' } : { name: 'ECDH', namedCurve: 'P-256' };
      const clientPub = await webcrypto.subtle.importKey(
        'spki',
        Buffer.from(body.clientPublicKey, 'base64url'),
        alg,
        false,
        []
      );
      const kp = await webcrypto.subtle.generateKey(alg, false, ['deriveBits']);
      const deriveAlg =
        suite === SUITE_X25519
          ? { name: 'X25519', public: clientPub }
          : { name: 'ECDH', public: clientPub };
      const shared = Buffer.from(await webcrypto.subtle.deriveBits(deriveAlg, kp.privateKey, 256));

      const sessionId = randomUUID();
      const serverNonce = randomBytes(24);
      const clientNonce = Buffer.from(body.clientNonce, 'base64url');
      assert.equal(clientNonce.length, 24, 'client nonce should be 24 bytes');

      // salt = SHA256(clientNonce || serverNonce || Guid.ToByteArray(sessionId))
      const salt = createHash('sha256')
        .update(Buffer.concat([clientNonce, serverNonce, dotnetGuidBytes(sessionId)]))
        .digest();

      const c2s = Buffer.from(hkdfSync('sha256', shared, salt, 'synaptix:c2s:v1', 32));
      const s2c = Buffer.from(hkdfSync('sha256', shared, salt, 'synaptix:s2c:v1', 32));

      const expires = new Date(Date.now() + 30 * 60 * 1000);
      if (forceZeroMs) expires.setMilliseconds(0);

      const idN = sessionId.replace(/-/g, '').toLowerCase();
      const serverPublicKey = b64url(
        Buffer.from(await webcrypto.subtle.exportKey('spki', kp.publicKey))
      );
      const advertised = body.supportedSuites.join('|');
      // Signature over "{id:N}:{serverPub}:{expires:O}:{suite}:{advertised}"
      const sigInput = `${idN}:${serverPublicKey}:${dotnetRoundTrip(expires)}:${suite}:${advertised}`;
      const signature = createHmac('sha256', s2c).update(sigInput, 'utf8').digest();
      if (tamperSignature) signature[0] ^= 0xff;

      Object.assign(state, { sessionId, idN, c2s, s2c });

      return {
        data: {
          sessionId,
          protocolVersion: 'syn-sec-v1',
          selectedSuite: suite,
          serverPublicKey,
          serverNonce: b64url(serverNonce),
          expiresAtUtc: stjDateTimeOffset(expires),
          serverSignature: b64url(signature),
        },
      };
    },

    // SecureChannelFilter.BuildAad + SecurePayloadService.DecryptAsync
    decryptRequest(envelope, headers, method, target) {
      assert.equal(headers['X-Syn-Sec-Session'], state.sessionId);
      assert.match(headers['X-Syn-Sec-Seq'], /^[1-9]\d*$/);
      assert.ok(headers['X-Syn-Sec-Nonce'].length > 0);
      assert.equal(envelope.contentType, 'application/json');
      const ageMs = Math.abs(Date.now() - Date.parse(envelope.encryptedAtUtc));
      assert.ok(ageMs < 5 * 60 * 1000, 'encryptedAtUtc within clock skew');

      const aad = `syn-sec-v1|request|${method}|${target}|${state.idN}|${headers['X-Syn-Sec-Seq']}|${SUBJECT}`;
      const d = createDecipheriv('aes-256-gcm', state.c2s, Buffer.from(envelope.nonce, 'base64url'));
      d.setAAD(Buffer.from(aad, 'utf8'));
      d.setAuthTag(Buffer.from(envelope.mac, 'base64url'));
      const plaintext = Buffer.concat([
        d.update(Buffer.from(envelope.ciphertext, 'base64url')),
        d.final(),
      ]);
      return JSON.parse(plaintext.toString('utf8'));
    },

    encryptResponse(obj, method, target, seq) {
      const aad = `syn-sec-v1|response|${method}|${target}|${state.idN}|${seq}|${SUBJECT}`;
      const nonce = randomBytes(12);
      const c = createCipheriv('aes-256-gcm', state.s2c, nonce);
      c.setAAD(Buffer.from(aad, 'utf8'));
      const ct = Buffer.concat([c.update(JSON.stringify(obj), 'utf8'), c.final()]);
      return {
        ciphertext: b64url(ct),
        nonce: b64url(nonce),
        mac: b64url(c.getAuthTag()),
        contentType: 'application/json',
        encryptedAtUtc: new Date().toISOString(),
      };
    },
  };
}

function makeChannel(server, suite) {
  return new SecureChannel({
    post: (url, data) => server.handlePost(url, data),
    getDeviceId: () => DEVICE,
    getSubjectId: () => SUBJECT,
    crypto: webcrypto,
    forceSuite: suite,
  });
}

async function testSuite(suite) {
  const server = makeServer(suite);
  const channel = makeChannel(server, suite);
  const target = '/api/v1/store/purchase';
  const body = { playerId: SUBJECT, sku: 'starter_pack', quantity: 1, currency: 'coins' };

  // Round trip 1
  const req1 = await channel.encryptRequest('POST', target, body);
  const serverSaw = server.decryptRequest(req1.envelope, req1.headers, 'POST', target);
  assert.deepEqual(serverSaw, body, `${suite}: server decrypts request`);
  assert.equal(req1.seq, 1);

  const responseObj = { status: 'Applied', newBalance: 900 };
  const respEnvelope = server.encryptResponse(responseObj, 'POST', target, req1.seq);
  const clientSaw = await channel.decryptResponse('POST', target, req1.seq, respEnvelope);
  assert.deepEqual(clientSaw, responseObj, `${suite}: client decrypts response`);

  // Sequence increments and second round trip works on the same session
  const req2 = await channel.encryptRequest('POST', target, { ping: 2 });
  assert.equal(req2.seq, 2, `${suite}: sequence increments`);
  assert.deepEqual(server.decryptRequest(req2.envelope, req2.headers, 'POST', target), { ping: 2 });

  // Tampered tag must fail
  const bad = { ...respEnvelope, mac: b64url(randomBytes(16)) };
  await assert.rejects(
    () => channel.decryptResponse('POST', target, req1.seq, bad),
    `${suite}: tampered mac rejected`
  );

  // AAD binding: decrypting under the wrong target must fail
  await assert.rejects(
    () => channel.decryptResponse('POST', '/api/v1/other', req1.seq, respEnvelope),
    `${suite}: AAD binds target path`
  );

  console.log(`✓ ${suite}: request/response round trips, seq, tamper + AAD rejection`);
}

async function testSignatureVerification() {
  const server = makeServer(SUITE_P256, { tamperSignature: true });
  const channel = makeChannel(server, SUITE_P256);
  await assert.rejects(
    () => channel.encryptRequest('POST', '/api/v1/store/purchase', {}),
    /signature verification failed/,
    'tampered transcript signature rejected'
  );
  console.log('✓ downgrade protection: tampered server signature rejected');
}

async function testZeroMillisecondExpiry() {
  // STJ emits no fraction when ms == 0; the client must still rebuild ".0000000"
  const server = makeServer(SUITE_P256, { forceZeroMs: true });
  const channel = makeChannel(server, SUITE_P256);
  const req = await channel.encryptRequest('POST', '/api/v1/store/purchase', { a: 1 });
  assert.deepEqual(server.decryptRequest(req.envelope, req.headers, 'POST', '/api/v1/store/purchase'), { a: 1 });
  console.log('✓ zero-millisecond expiry timestamp round trip');
}

function testEncodingHelpers() {
  // Known .NET vector: Guid("00112233-4455-6677-8899-aabbccddeeff").ToByteArray()
  assert.deepEqual(
    Buffer.from(guidToDotNetBytes('00112233-4455-6677-8899-aabbccddeeff')),
    Buffer.from('33221100554477668899aabbccddeeff', 'hex'),
    'Guid.ToByteArray byte order'
  );

  assert.equal(toDotNetRoundTrip('2026-07-19T12:34:56.123+00:00'), '2026-07-19T12:34:56.1230000+00:00');
  assert.equal(toDotNetRoundTrip('2026-07-19T12:34:56+00:00'), '2026-07-19T12:34:56.0000000+00:00');
  assert.equal(toDotNetRoundTrip('2026-07-19T12:34:56.1234567+00:00'), '2026-07-19T12:34:56.1234567+00:00');
  assert.equal(toDotNetRoundTrip('2026-07-19T12:34:56.5Z'), '2026-07-19T12:34:56.5000000+00:00');
  console.log('✓ encoding helpers (.NET Guid bytes, round-trip timestamp format)');
}

testEncodingHelpers();
await testSuite(SUITE_P256);
if (await webcrypto.subtle.generateKey({ name: 'X25519' }, false, ['deriveBits']).then(() => true).catch(() => false)) {
  await testSuite(SUITE_X25519);
} else {
  console.log('- X25519 not supported by this runtime; suite skipped');
}
await testSignatureVerification();
await testZeroMillisecondExpiry();
console.log('\nAll secure-channel interop tests passed.');
