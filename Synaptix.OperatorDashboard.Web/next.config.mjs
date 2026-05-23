/** @type {import('next').NextConfig} */
const nextConfig = {
  basePath: process.env.BASEPATH,

  // Produce a standalone build for Docker (includes server.js + dependencies)
  output: 'standalone',

  // Proxy API requests to the backend during local development.
  // In production, set NEXT_PUBLIC_API_URL to the backend URL and requests go direct.
  async rewrites() {
    const backendUrl = process.env.BACKEND_URL ?? 'http://localhost:5000'

    return [
      {
        source: '/api/backend/:path*',
        destination: `${backendUrl}/:path*`
      }
    ]
  }
}

export default nextConfig
