import { jsx as _jsx } from "react/jsx-runtime";
import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './app/App';
const rootElement = document.getElementById('root');
if (!rootElement)
    throw new Error('Failed to find the root element');
ReactDOM.createRoot(rootElement).render(_jsx(React.StrictMode, { children: _jsx(App, {}) }));
