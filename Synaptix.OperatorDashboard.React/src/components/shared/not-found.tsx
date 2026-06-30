/**
 * 404 Not Found page
 */

// import React from 'react'
import { Link } from 'react-router-dom'

export default function NotFoundPage() {
  return (
    <div className="min-h-screen bg-bg-primary flex items-center justify-center px-4">
      <div className="text-center space-y-6">
        <div>
          <h1 className="text-6xl font-bold text-accent">404</h1>
          <p className="text-2xl font-semibold text-ink-primary mt-2">Page not found</p>
        </div>
        <p className="text-ink-secondary">Sorry, we couldn't find the page you're looking for.</p>
        <Link to="/" className="inline-block px-6 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark transition-smooth">
          Go back home
        </Link>
      </div>
    </div>
  )
}
