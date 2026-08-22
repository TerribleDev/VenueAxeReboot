import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: './openapi.json',
  output: './src/lib/api/generated',
  client: '@hey-api/client-fetch'
});
