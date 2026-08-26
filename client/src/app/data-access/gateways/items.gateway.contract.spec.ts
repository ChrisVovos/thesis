import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { InMemoryCache } from '@apollo/client/core';
import {
  APOLLO_TESTING_CACHE,
  ApolloTestingController,
  ApolloTestingModule,
} from 'apollo-angular/testing';
import { firstValueFrom } from 'rxjs';
import { ItemsGateway } from './items.gateway';
import { GraphQlItemsGateway } from '../graphql/graphql-items.gateway';
import { RestItemsGateway } from '../rest/rest-items.gateway';
import { SEARCH_ITEMS } from '../graphql/item.documents';
import type { ItemSummary } from '../../shared/models/item.models';

/**
 * The shared contract suite for {@link ItemsGateway}.
 *
 * The same expectations are executed against both implementations. Each implementation is fed the
 * wire format its own transport produces, and both must yield byte-identical view models. This is the
 * test that makes the comparison trustworthy: if the two ever disagreed on what an item is, every
 * measurement taken afterwards would be comparing different things.
 */
describe('ItemsGateway contract', () => {
  const expectedSummary: ItemSummary = {
    id: '018f0d4f-0000-7000-8000-000000000001',
    type: 'MultipleChoiceSingleResponse',
    status: 'Published',
    difficulty: 'Easy',
    stem: 'Which of the following is a prime number?',
    maximumScore: 1,
    categoryId: '018f0d4f-0000-7000-8000-000000000002',
    categoryName: 'Mathematics',
    authorId: '018f0d4f-0000-7000-8000-000000000003',
    authorName: 'Test Author',
    versionNumber: 1,
    createdAtUtc: '2026-08-01T09:00:00.000Z',
    lastModifiedAtUtc: null,
    tags: [{ id: '018f0d4f-0000-7000-8000-000000000004', name: 'algebra' }],
  };

  describe('REST implementation', () => {
    let gateway: ItemsGateway;
    let http: HttpTestingController;

    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          { provide: ItemsGateway, useExisting: RestItemsGateway },
        ],
      });

      gateway = TestBed.inject(ItemsGateway);
      http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('maps a page of items to the shared view model', async () => {
      const pending = firstValueFrom(gateway.search({ page: 1, pageSize: 20 }));

      const request = http.expectOne((candidate) => candidate.url.endsWith('/items'));
      expect(request.request.params.get('page')).toBe('1');
      request.flush({ items: [expectedSummary], totalCount: 1, page: 1, pageSize: 20 });

      const page = await pending;
      expect(page.items).toEqual([expectedSummary]);
      expect(page.totalCount).toBe(1);
      expect(page.hasNextPage).toBe(false);
    });

    it('translates every filter into query parameters', async () => {
      const pending = firstValueFrom(
        gateway.search({
          page: 2,
          pageSize: 10,
          search: 'prime',
          statuses: ['Published'],
          types: ['Essay'],
          tagIds: ['tag-a', 'tag-b'],
        }),
      );

      const request = http.expectOne((candidate) => candidate.url.endsWith('/items'));
      expect(request.request.params.get('search')).toBe('prime');
      expect(request.request.params.getAll('status')).toEqual(['Published']);
      expect(request.request.params.getAll('type')).toEqual(['Essay']);
      expect(request.request.params.getAll('tagId')).toEqual(['tag-a', 'tag-b']);
      request.flush({ items: [], totalCount: 0, page: 2, pageSize: 10 });

      await pending;
    });
  });

  describe('GraphQL implementation', () => {
    let gateway: ItemsGateway;
    let backend: ApolloTestingController;

    beforeEach(() => {
      TestBed.configureTestingModule({
        imports: [ApolloTestingModule],
        providers: [
          // The production client also disables __typename, so the documents under test are the ones
          // the application really sends.
          { provide: APOLLO_TESTING_CACHE, useValue: new InMemoryCache({ addTypename: false }) },
          { provide: ItemsGateway, useExisting: GraphQlItemsGateway },
        ],
      });

      gateway = TestBed.inject(ItemsGateway);
      backend = TestBed.inject(ApolloTestingController);
    });

    afterEach(() => backend.verify());

    it('maps a page of items to the same shared view model', async () => {
      const pending = firstValueFrom(gateway.search({ page: 1, pageSize: 20 }));

      const operation = backend.expectOne(SEARCH_ITEMS);
      operation.flush({
        data: {
          searchItems: {
            totalCount: 1,
            page: 1,
            pageSize: 20,
            items: [
              {
                ...expectedSummary,
                type: 'MULTIPLE_CHOICE_SINGLE_RESPONSE',
                status: 'PUBLISHED',
                difficulty: 'EASY',
              },
            ],
          },
        },
      });

      const page = await pending;
      expect(page.items).toEqual([expectedSummary]);
      expect(page.totalCount).toBe(1);
      expect(page.hasNextPage).toBe(false);
    });

    it('translates client enum names into the values the schema declares', async () => {
      const pending = firstValueFrom(
        gateway.search({ page: 1, pageSize: 20, statuses: ['InReview'], types: ['EitherOr'] }),
      );

      const operation = backend.expectOne(SEARCH_ITEMS);
      const criteria = operation.operation.variables['criteria'] as {
        statuses: string[];
        types: string[];
      };

      expect(criteria.statuses).toEqual(['IN_REVIEW']);
      expect(criteria.types).toEqual(['EITHER_OR']);

      operation.flush({
        data: { searchItems: { totalCount: 0, page: 1, pageSize: 20, items: [] } },
      });

      await pending;
    });

    it('surfaces a server error in the shared normalized shape', async () => {
      const pending = firstValueFrom(gateway.search({ page: 1, pageSize: 20 }));

      backend.expectOne(SEARCH_ITEMS).graphqlErrors([
        {
          message: 'The operation requires the items.read permission.',
          extensions: { code: 'auth.forbidden', classification: 'FORBIDDEN' },
        },
      ]);

      await expect(pending).rejects.toMatchObject({
        code: 'auth.forbidden',
        kind: 'forbidden',
      });
    });
  });
});
