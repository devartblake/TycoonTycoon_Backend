import FeatureUnavailable from '@/components/shared/feature-unavailable'

export default function InstallerUnavailablePage() {
  return (
    <FeatureUnavailable
      title="Setup / installer disabled"
      reason="The React installer UI is not wired to production-ready admin APIs. Use the Setup CLI and migration service for bootstrap."
      alternatives={[
        {
          label: 'Local happy path',
          note: 'make dev  ·  scripts/bootstrap-local.ps1  ·  docs/setup/LOCAL_DEV_HAPPY_PATH.md',
        },
        {
          label: 'Setup CLI',
          note: 'dotnet run --project Synaptix.Setup -- init-local',
        },
      ]}
    />
  )
}
