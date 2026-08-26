import { HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { normalizeGraphQlErrors, normalizeHttpError } from './error-normalizer';

/**
 * The two surfaces report failures in different formats. These tests pin down that they arrive at the
 * user interface in the same shape, which is what allows every screen to be written once.
 */
describe('error normalization', () => {
  it('maps a problem document to the shared shape', () => {
    const error = normalizeHttpError(
      new HttpErrorResponse({
        status: 409,
        error: {
          code: 'item.not_editable',
          title: 'The request conflicts with the current state of the resource.',
          detail: 'An item in status Published cannot be edited.',
          correlationId: 'abc123',
        },
      }),
    );

    expect(error).toEqual({
      code: 'item.not_editable',
      message: 'An item in status Published cannot be edited.',
      kind: 'conflict',
      fieldErrors: undefined,
      correlationId: 'abc123',
    });
  });

  it('maps GraphQL error extensions to the same shape', () => {
    const error = normalizeGraphQlErrors(
      [
        {
          message: 'An item in status Published cannot be edited.',
          extensions: { code: 'item.not_editable', classification: 'CONFLICT' },
        },
      ],
      'abc123',
    );

    expect(error).toEqual({
      code: 'item.not_editable',
      message: 'An item in status Published cannot be edited.',
      kind: 'conflict',
      fieldErrors: undefined,
      correlationId: 'abc123',
    });
  });

  it('carries per-field details through from a validation failure', () => {
    const error = normalizeHttpError(
      new HttpErrorResponse({
        status: 400,
        error: {
          code: 'validation.failed',
          detail: 'One or more input values are invalid.',
          errors: { Stem: ['A stem is required.'] },
        },
      }),
    );

    expect(error.kind).toBe('validation');
    expect(error.fieldErrors).toEqual({ Stem: ['A stem is required.'] });
  });

  it('reports an unreachable server distinctly from a rejected request', () => {
    const error = normalizeHttpError(new HttpErrorResponse({ status: 0 }));

    expect(error.code).toBe('network.unreachable');
    expect(error.kind).toBe('failure');
  });

  it('falls back to the response headers for the correlation identifier', () => {
    const error = normalizeHttpError(
      new HttpErrorResponse({
        status: 404,
        error: { code: 'item.not_found', detail: 'The item does not exist.' },
        headers: new HttpHeaders({ 'X-Correlation-Id': 'from-header' }),
      }),
    );

    expect(error.correlationId).toBe('from-header');
  });

  it('degrades gracefully when GraphQL reports nothing useful', () => {
    expect(normalizeGraphQlErrors([]).code).toBe('graphql.unknown');
  });
});
