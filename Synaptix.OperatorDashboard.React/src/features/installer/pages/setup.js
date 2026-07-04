import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
/**
 * Backend Installer & Setup - Installation wizard, bundle management, health monitoring
 */
import { useState, useEffect } from 'react';
import { usePermission } from '@/hooks/use-permission';
import ErrorBoundary from '@/components/shared/error-boundary';
import EmptyState from '@/components/shared/empty-state';
import { SkeletonGrid, SkeletonList } from '@/components/shared/skeletons';
import * as installerApi from '../api';
export default function SetupPage() {
    usePermission('config:write');
    const [activeTab, setActiveTab] = useState('installer');
    const [installStatus, setInstallStatus] = useState(null);
    const [bundles, setBundles] = useState([]);
    const [health, setHealth] = useState(null);
    const [loading, setLoading] = useState(true);
    const [uploadProgress, setUploadProgress] = useState(0);
    const [successMsg, setSuccessMsg] = useState(null);
    const [errorMsg, setErrorMsg] = useState(null);
    const [config, setConfig] = useState({
        environment: 'production',
        apiPort: 5000,
        enableMonitoring: true,
        enableSentry: true,
        logLevel: 'info',
    });
    useEffect(() => {
        const loadData = async () => {
            setLoading(true);
            try {
                const [status, bundlesList, healthData, conf] = await Promise.all([
                    installerApi.getInstallationStatus(),
                    installerApi.getAvailableBundles(),
                    installerApi.getBackendHealth(),
                    installerApi.getInstallerConfig(),
                ]);
                setInstallStatus(status);
                setBundles(bundlesList);
                setHealth(healthData);
                setConfig(conf);
            }
            catch (error) {
                console.error('Failed to load installer data:', error);
                setErrorMsg('Failed to load installer data');
            }
            finally {
                setLoading(false);
            }
        };
        loadData();
        // Poll for updates
        const interval = setInterval(loadData, 3000);
        return () => clearInterval(interval);
    }, []);
    const handleStartInstallation = async () => {
        if (!confirm('Start backend installation? This will restart services.'))
            return;
        try {
            await installerApi.startInstallation(config);
            setSuccessMsg('Installation started');
            setTimeout(() => setSuccessMsg(null), 2000);
        }
        catch (error) {
            setErrorMsg(error instanceof Error ? error.message : 'Installation failed');
        }
    };
    const handleBundleUpload = async (e) => {
        const file = e.target.files?.[0];
        if (!file)
            return;
        try {
            setUploadProgress(0);
            await installerApi.uploadBundle(file, (progress) => setUploadProgress(progress));
            setSuccessMsg('Bundle uploaded successfully');
            setTimeout(() => setSuccessMsg(null), 2000);
            // Reload bundles
            const updated = await installerApi.getAvailableBundles();
            setBundles(updated);
        }
        catch (error) {
            setErrorMsg(error instanceof Error ? error.message : 'Upload failed');
        }
    };
    const handleDeployBundle = async (bundleId) => {
        if (!confirm('Deploy this bundle? Services will restart.'))
            return;
        try {
            await installerApi.deployBundle(bundleId);
            setSuccessMsg('Bundle deployed successfully');
            setTimeout(() => setSuccessMsg(null), 2000);
        }
        catch (error) {
            setErrorMsg(error instanceof Error ? error.message : 'Deployment failed');
        }
    };
    const handleValidateEnvironment = async () => {
        try {
            const result = await installerApi.validateEnvironment();
            if (result.valid) {
                setSuccessMsg('✅ Environment validation passed');
            }
            else {
                setErrorMsg(`❌ Issues found:\n${result.issues.join('\n')}`);
            }
            setTimeout(() => {
                setSuccessMsg(null);
                setErrorMsg(null);
            }, 4000);
        }
        catch (error) {
            setErrorMsg('Validation failed');
        }
    };
    const handleRestartBackend = async () => {
        if (!confirm('Restart backend? Services will be temporarily unavailable.'))
            return;
        try {
            const result = await installerApi.restartBackend();
            setSuccessMsg(`Backend restarting (estimated downtime: ${result.estimatedDowntime}s)`);
            setTimeout(() => setSuccessMsg(null), 2000);
        }
        catch (error) {
            setErrorMsg('Restart failed');
        }
    };
    return (_jsx(ErrorBoundary, { children: _jsxs("div", { className: "operator-container space-y-8", children: [_jsxs("div", { children: [_jsx("h1", { className: "text-3xl font-bold text-ink-primary", children: "Backend Installer & Setup" }), _jsx("p", { className: "mt-2 text-ink-secondary", children: "Configure, deploy, and monitor backend installation" })] }), successMsg && (_jsxs("div", { className: "p-4 bg-status-healthy/10 border border-status-healthy/20 rounded text-status-healthy text-sm", children: ["\u2713 ", successMsg] })), errorMsg && (_jsx("div", { className: "p-4 bg-status-offline/10 border border-status-offline/20 rounded text-status-offline text-sm whitespace-pre-wrap", children: errorMsg })), loading ? (_jsx(SkeletonGrid, { count: 4 })) : health ? (_jsxs("div", { className: "grid grid-cols-4 gap-4", children: [_jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Backend Status" }), _jsx("p", { className: `text-2xl font-bold mt-1 ${health.status === 'healthy' ? 'text-status-healthy' : health.status === 'degraded' ? 'text-status-degraded' : 'text-status-offline'}`, children: health.status.toUpperCase() })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Uptime" }), _jsxs("p", { className: "text-xl font-bold text-accent mt-1", children: [Math.floor(health.uptime / 3600), "h ", Math.floor((health.uptime % 3600) / 60), "m"] })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Database" }), _jsx("p", { className: `text-lg font-bold mt-1 ${health.database.status === 'connected' ? 'text-status-healthy' : 'text-status-offline'}`, children: health.database.status }), _jsxs("p", { className: "text-xs text-ink-tertiary mt-1", children: [health.database.latency, "ms"] })] }), _jsxs("div", { className: "operator-card", children: [_jsx("p", { className: "text-xs text-ink-tertiary", children: "Services" }), _jsxs("p", { className: "text-2xl font-bold text-ink-primary mt-1", children: [health.services.filter(s => s.status === 'running').length, "/", health.services.length] })] })] })) : null, _jsx("div", { className: "flex gap-2 border-b border-panel-border", children: [
                        { id: 'installer', label: '🔧 Installation' },
                        { id: 'bundles', label: '📦 Bundle Manager' },
                        { id: 'health', label: '❤️ Health & Monitoring' },
                    ].map((tab) => (_jsx("button", { onClick: () => setActiveTab(tab.id), className: `px-4 py-2 font-medium border-b-2 transition-colors ${activeTab === tab.id
                            ? 'border-accent text-accent'
                            : 'border-transparent text-ink-secondary hover:text-ink-primary'}`, children: tab.label }, tab.id))) }), _jsx("div", { className: "operator-card", children: loading ? (_jsx("div", { className: "space-y-4 p-4", children: _jsx(SkeletonList, { items: 3 }) })) : activeTab === 'installer' ? (_jsxs("div", { className: "space-y-6", children: [_jsx("h2", { className: "text-lg font-semibold", children: "Installation Wizard" }), installStatus && (_jsxs("div", { className: "space-y-3", children: [_jsxs("div", { className: "bg-panel p-4 rounded", children: [_jsxs("div", { className: "flex items-center justify-between mb-2", children: [_jsx("span", { className: "font-semibold", children: "Progress" }), _jsxs("span", { className: "text-sm text-ink-secondary", children: [installStatus.progress, "%"] })] }), _jsx("div", { className: "w-full bg-panel-border rounded-full h-2", children: _jsx("div", { className: "bg-accent h-2 rounded-full transition-all", style: { width: `${installStatus.progress}%` } }) })] }), _jsx("div", { className: "space-y-2", children: installStatus.steps.map((step, idx) => (_jsx("div", { className: "p-3 border border-panel-border rounded hover:bg-panel/50", children: _jsxs("div", { className: "flex items-start justify-between", children: [_jsxs("div", { className: "flex-1", children: [_jsxs("p", { className: "font-semibold text-ink-primary", children: [idx + 1, ". ", step.name] }), _jsx("p", { className: "text-sm text-ink-secondary", children: step.description }), step.details && _jsx("p", { className: "text-xs text-ink-tertiary mt-1", children: step.details })] }), _jsx("span", { className: `px-2 py-1 rounded text-xs font-medium whitespace-nowrap ${step.status === 'completed' ? 'bg-status-healthy/20 text-status-healthy' :
                                                            step.status === 'running' ? 'bg-accent/20 text-accent animate-pulse' :
                                                                step.status === 'failed' ? 'bg-status-offline/20 text-status-offline' :
                                                                    'bg-panel text-ink-secondary'}`, children: step.status })] }) }, step.id))) })] })), _jsxs("div", { className: "border-t border-panel-border pt-6", children: [_jsx("h3", { className: "font-semibold mb-4", children: "Configuration" }), _jsxs("div", { className: "grid grid-cols-2 gap-4", children: [_jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Environment" }), _jsxs("select", { value: config.environment || 'production', onChange: (e) => setConfig({ ...config, environment: e.target.value }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel", children: [_jsx("option", { children: "development" }), _jsx("option", { children: "staging" }), _jsx("option", { children: "production" })] })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "API Port" }), _jsx("input", { type: "number", value: config.apiPort || 5000, onChange: (e) => setConfig({ ...config, apiPort: Number(e.target.value) }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel" })] }), _jsxs("div", { children: [_jsx("label", { className: "text-sm font-medium", children: "Log Level" }), _jsxs("select", { value: config.logLevel || 'info', onChange: (e) => setConfig({ ...config, logLevel: e.target.value }), className: "w-full mt-1 px-3 py-2 border border-panel-border rounded bg-panel", children: [_jsx("option", { children: "debug" }), _jsx("option", { children: "info" }), _jsx("option", { children: "warn" }), _jsx("option", { children: "error" })] })] }), _jsxs("div", { className: "flex items-center gap-2 pt-6", children: [_jsx("input", { type: "checkbox", checked: config.enableMonitoring, onChange: (e) => setConfig({ ...config, enableMonitoring: e.target.checked }) }), _jsx("label", { className: "text-sm", children: "Enable Monitoring" })] })] })] }), _jsxs("div", { className: "flex gap-2 pt-4 border-t border-panel-border", children: [_jsx("button", { onClick: handleStartInstallation, disabled: installStatus?.status === 'in-progress', className: "px-4 py-2 bg-accent text-white rounded font-medium hover:bg-accent-dark disabled:opacity-50", children: "Start Installation" }), _jsx("button", { onClick: handleValidateEnvironment, className: "px-4 py-2 bg-panel hover:bg-panel-border rounded font-medium", children: "Validate Environment" })] })] })) : activeTab === 'bundles' ? (_jsxs("div", { className: "space-y-6", children: [_jsx("h2", { className: "text-lg font-semibold", children: "Bundle Management" }), _jsx("div", { className: "p-4 border-2 border-dashed border-panel-border rounded", children: _jsxs("div", { className: "text-center", children: [_jsx("p", { className: "text-sm font-semibold mb-2", children: "Upload Backend Bundle" }), _jsx("input", { type: "file", accept: ".zip,.tar.gz", onChange: handleBundleUpload, className: "hidden", id: "bundle-upload" }), _jsx("label", { htmlFor: "bundle-upload", className: "inline-block px-4 py-2 bg-accent text-white rounded cursor-pointer hover:bg-accent-dark", children: "Choose File" }), uploadProgress > 0 && uploadProgress < 100 && (_jsxs("div", { className: "mt-4", children: [_jsx("div", { className: "bg-panel p-2 rounded", children: _jsx("div", { className: "bg-accent h-2 rounded transition-all", style: { width: `${uploadProgress}%` } }) }), _jsxs("p", { className: "text-xs text-ink-secondary mt-2", children: [uploadProgress, "% uploaded"] })] }))] }) }), _jsx("div", { className: "space-y-3", children: bundles.length > 0 ? (bundles.map((bundle) => (_jsx("div", { className: "p-4 border border-panel-border rounded hover:bg-panel/50", children: _jsxs("div", { className: "flex items-start justify-between", children: [_jsxs("div", { className: "flex-1", children: [_jsxs("p", { className: "font-semibold text-ink-primary", children: [bundle.name, " v", bundle.version] }), _jsx("p", { className: "text-sm text-ink-secondary", children: bundle.notes }), _jsxs("div", { className: "flex gap-4 mt-2 text-xs text-ink-tertiary", children: [_jsxs("span", { children: ["Size: ", (bundle.fileSize / 1024 / 1024).toFixed(1), " MB"] }), _jsxs("span", { children: ["Released: ", new Date(bundle.releaseDate).toLocaleDateString()] }), bundle.breaking && _jsx("span", { className: "text-status-offline", children: "\u26A0\uFE0F Breaking changes" })] })] }), _jsx("button", { onClick: () => handleDeployBundle(bundle.id), className: "px-4 py-2 bg-status-healthy/20 text-status-healthy rounded font-medium hover:bg-status-healthy/30 whitespace-nowrap", children: "Deploy" })] }) }, bundle.id)))) : (_jsx(EmptyState, { title: "No bundles available", description: "Upload a backend bundle to deploy updates", icon: "\uD83D\uDCE6", action: { label: 'Upload Bundle', onClick: () => document.getElementById('bundle-upload')?.click() } })) })] })) : (_jsxs("div", { className: "space-y-6", children: [_jsx("h2", { className: "text-lg font-semibold", children: "Backend Health & Monitoring" }), health && (_jsxs("div", { className: "space-y-3", children: [_jsx("h3", { className: "font-semibold", children: "Service Status" }), health.services.map((service) => (_jsxs("div", { className: "p-3 border border-panel-border rounded", children: [_jsxs("div", { className: "flex items-center justify-between mb-2", children: [_jsx("span", { className: "font-medium", children: service.name }), _jsx("span", { className: `px-2 py-1 rounded text-xs font-medium ${service.status === 'running' ? 'bg-status-healthy/20 text-status-healthy' : 'bg-status-offline/20 text-status-offline'}`, children: service.status })] }), _jsxs("div", { className: "grid grid-cols-2 gap-4 text-sm", children: [_jsxs("div", { children: [_jsx("p", { className: "text-ink-tertiary", children: "CPU Usage" }), _jsxs("p", { className: "font-semibold", children: [service.cpu, "%"] })] }), _jsxs("div", { children: [_jsx("p", { className: "text-ink-tertiary", children: "Memory Usage" }), _jsxs("p", { className: "font-semibold", children: [service.memory, "%"] })] })] })] }, service.name)))] })), _jsx("div", { className: "flex gap-2 pt-4 border-t border-panel-border", children: _jsx("button", { onClick: handleRestartBackend, className: "px-4 py-2 bg-status-offline/20 text-status-offline rounded font-medium hover:bg-status-offline/30", children: "Restart Backend" }) })] })) }), _jsxs("div", { className: "p-4 bg-ink-secondary/5 border border-panel-border rounded text-xs text-ink-tertiary", children: [_jsx("p", { className: "font-medium text-ink-secondary mb-2", children: "\u2705 Backend Installer Complete" }), _jsxs("ul", { className: "space-y-1", children: [_jsx("li", { children: "\u2713 Installation wizard with step-by-step configuration" }), _jsx("li", { children: "\u2713 Backend bundle upload and deployment" }), _jsx("li", { children: "\u2713 Health monitoring and service status tracking" }), _jsx("li", { children: "\u2713 Environment validation and configuration management" }), _jsx("li", { children: "\u2713 Real-time progress tracking" })] })] })] }) }));
}
