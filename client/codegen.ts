import type { CodegenConfig } from '@graphql-codegen/cli';

/**
 * Generates strongly typed documents for every GraphQL operation the client sends.
 *
 * The schema is read from the checked-in artefact rather than from a running server so that codegen,
 * and therefore the build, is reproducible offline. Regenerate the artefact with
 * `dotnet run --project src/ItemAuthoring.Api -- --export-contracts artifacts`.
 */
const config: CodegenConfig = {
  schema: '../artifacts/schema.graphql',
  // The operations live in TypeScript files as tagged templates, so codegen extracts them from there
  // and validates every one of them against the schema the server actually publishes.
  documents: [
    'src/app/data-access/graphql/**/*.documents.ts',
    '!src/app/data-access/graphql/generated/**',
  ],
  ignoreNoDocuments: true,
  generates: {
    'src/app/data-access/graphql/generated/graphql.ts': {
      // `typescript-operations` v7 already emits the schema's input and enum types, so listing the
      // `typescript` plugin as well would declare every one of them twice.
      plugins: ['typescript-operations', 'typed-document-node'],
      config: {
        skipTypename: true,
        enumsAsTypes: true,
        scalars: {
          UUID: 'string',
          DateTime: 'string',
          Decimal: 'number',
        },
      },
    },
  },
};

export default config;
