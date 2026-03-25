<script lang="ts" setup>
import VerticalNavSectionTitle from '@/@layouts/components/VerticalNavSectionTitle.vue'
import VerticalNavLink from '@layouts/components/VerticalNavLink.vue'
</script>

<template>
  <!-- Dashboard -->
  <VerticalNavLink
    :item="{
      title: 'Dashboard',
      icon: 'ri-home-smile-line',
      to: '/dashboard',
    }"
  />

  <!-- Operations -->
  <VerticalNavSectionTitle
    :item="{
      heading: 'Operations',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Users',
      icon: 'ri-group-line',
      to: '/users',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Season Points',
      icon: 'ri-trophy-line',
      to: '/season-points',
    }"
  />

  <!-- Moderation -->
  <VerticalNavSectionTitle
    :item="{
      heading: 'Moderation',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Anti-Cheat',
      icon: 'ri-shield-line',
      to: '/anti-cheat',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Escalations',
      icon: 'ri-alarm-warning-line',
      to: '/escalations',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Moderation Log',
      icon: 'ri-file-list-line',
      to: '/moderation',
    }"
  />

  <!-- Economy -->
  <VerticalNavSectionTitle
    :item="{
      heading: 'Economy',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Economy',
      icon: 'ri-money-dollar-circle-line',
      to: '/economy',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Transactions',
      icon: 'ri-exchange-line',
      to: '/player-transactions',
    }"
  />

  <!-- Communications -->
  <VerticalNavSectionTitle
    :item="{
      heading: 'Communications',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Notifications',
      icon: 'ri-notification-line',
      to: '/notifications',
    }"
  />

  <!-- System -->
  <VerticalNavSectionTitle
    :item="{
      heading: 'System',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Observability',
      icon: 'ri-pulse-line',
      to: '/observability',
    }"
  />
  <VerticalNavLink
    :item="{
      title: 'Security Audit',
      icon: 'ri-lock-line',
      to: '/security',
    }"
  />
</template>
