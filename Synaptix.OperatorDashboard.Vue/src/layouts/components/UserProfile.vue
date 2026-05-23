<script setup lang="ts">
import { getAdmin, logout } from '@/lib/auth'

const admin = computed(() => getAdmin())

function handleLogout() {
  logout()
}
</script>

<template>
  <VBadge
    dot
    location="bottom right"
    offset-x="3"
    offset-y="3"
    color="success"
    bordered
  >
    <VAvatar
      class="cursor-pointer"
      color="primary"
      variant="tonal"
    >
      <VIcon icon="ri-admin-line" />

      <VMenu
        activator="parent"
        width="230"
        location="bottom end"
        offset="14px"
      >
        <VList>
          <VListItem>
            <VListItemTitle class="font-weight-semibold">
              {{ admin?.displayName ?? 'Admin' }}
            </VListItemTitle>
            <VListItemSubtitle>{{ admin?.email ?? '' }}</VListItemSubtitle>
          </VListItem>
          <VDivider class="my-2" />

          <VListItem @click="handleLogout">
            <template #prepend>
              <VIcon
                class="me-2"
                icon="ri-logout-box-r-line"
                size="22"
              />
            </template>

            <VListItemTitle>Logout</VListItemTitle>
          </VListItem>
        </VList>
      </VMenu>
    </VAvatar>
  </VBadge>
</template>
