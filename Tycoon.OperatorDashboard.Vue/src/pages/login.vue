<script setup lang="ts">
import { login } from '@/lib/auth'
import { handleApiError } from '@/lib/apiErrors'
import type { ErrorHandlerResult } from '@/lib/apiErrors'
import logo from '@images/logo.svg?raw'

const router = useRouter()

const form = ref({
  email: '',
  password: '',
})

const isPasswordVisible = ref(false)
const loading = ref(false)
const error = ref<ErrorHandlerResult | null>(null)

async function onSubmit() {
  loading.value = true
  error.value = null

  try {
    await login(form.value.email, form.value.password)
    router.push('/')
  }
  catch (err) {
    error.value = handleApiError(err)
  }
  finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-wrapper d-flex align-center justify-center pa-4">
    <VCard
      class="auth-card pa-4 pt-7"
      max-width="448"
    >
      <VCardItem class="justify-center">
        <div class="d-flex align-center gap-3">
          <!-- eslint-disable vue/no-v-html -->
          <div
            class="d-flex"
            v-html="logo"
          />
          <h2 class="font-weight-medium text-2xl text-uppercase">
            Tycoon Ops
          </h2>
        </div>
      </VCardItem>

      <VCardText class="pt-2">
        <h4 class="text-h4 mb-1">
          Welcome back
        </h4>
        <p class="mb-0">
          Sign in to the operator dashboard
        </p>
      </VCardText>

      <VCardText>
        <VAlert
          v-if="error"
          :type="error.severity"
          closable
          class="mb-4"
          @click:close="error = null"
        >
          {{ error.message }}
        </VAlert>

        <VForm @submit.prevent="onSubmit">
          <VRow>
            <VCol cols="12">
              <VTextField
                v-model="form.email"
                label="Email"
                type="email"
                :disabled="loading"
              />
            </VCol>

            <VCol cols="12">
              <VTextField
                v-model="form.password"
                label="Password"
                placeholder="············"
                :type="isPasswordVisible ? 'text' : 'password'"
                autocomplete="password"
                :disabled="loading"
                :append-inner-icon="isPasswordVisible ? 'ri-eye-off-line' : 'ri-eye-line'"
                @click:append-inner="isPasswordVisible = !isPasswordVisible"
              />

              <VBtn
                block
                type="submit"
                class="mt-6"
                :loading="loading"
                :disabled="loading || !form.email || !form.password"
              >
                Login
              </VBtn>
            </VCol>
          </VRow>
        </VForm>
      </VCardText>
    </VCard>
  </div>
</template>

<style lang="scss">
@use "@core/scss/template/pages/page-auth";
</style>
