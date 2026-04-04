<template>
  <div class="layout">
    <aside class="nav">
      <h1>Operator Dashboard</h1>
      <router-link to="/dashboard">Dashboard</router-link>
      <router-link to="/audit-log">Audit Log</router-link>
      <router-link to="/users">Users</router-link>
    </aside>

    <main class="content">
      <router-view />
    </main>
  </div>
</template>

<style scoped>
.layout { display: grid; grid-template-columns: 240px 1fr; min-height: 100vh; font-family: Inter, system-ui, sans-serif; }
.nav { background: #0f172a; color: #e2e8f0; padding: 1rem; display: flex; flex-direction: column; gap: .75rem; }
.nav a { color: #cbd5e1; text-decoration: none; }
.nav a.router-link-active { color: white; font-weight: 600; }
.content { padding: 1rem; background: #f8fafc; }
</style>
