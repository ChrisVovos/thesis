import { inject, type Provider } from '@angular/core';
import { TransportService } from '../core/transport/transport.service';
import { AuthGateway } from './gateways/auth.gateway';
import { ExamsGateway } from './gateways/exams.gateway';
import { ItemsGateway, TaxonomyGateway } from './gateways/items.gateway';
import { UsersGateway } from './gateways/users.gateway';
import { GraphQlAuthGateway } from './graphql/graphql-auth.gateway';
import { GraphQlExamsGateway } from './graphql/graphql-exams.gateway';
import { GraphQlItemsGateway, GraphQlTaxonomyGateway } from './graphql/graphql-items.gateway';
import { GraphQlUsersGateway } from './graphql/graphql-users.gateway';
import { RestAuthGateway } from './rest/rest-auth.gateway';
import { RestExamsGateway } from './rest/rest-exams.gateway';
import { RestItemsGateway, RestTaxonomyGateway } from './rest/rest-items.gateway';
import { RestUsersGateway } from './rest/rest-users.gateway';

/**
 * Builds a gateway that forwards every call to whichever implementation is currently selected.
 *
 * The transport is resolved per call rather than per injection, which is what allows the toolbar
 * selector to change the behaviour of an already constructed component without recreating it.
 *
 * Forwarding is done with a proxy rather than by hand writing one delegating method per contract
 * member. That is a correctness decision, not a brevity one: a hand written router that forgot a
 * method would silently keep sending it over one transport, and the resulting measurements would be
 * wrong in a way no test would notice. A proxy cannot forget.
 *
 * @param transport The service that owns the active transport.
 * @param rest The REST implementation.
 * @param graphql The GraphQL implementation.
 * @returns A gateway that satisfies the contract and routes at call time.
 */
export function createTransportRouter<TGateway extends object>(
  transport: TransportService,
  rest: TGateway,
  graphql: TGateway,
): TGateway {
  return new Proxy(rest, {
    get(_target, property, receiver) {
      const active = transport.active() === 'graphql' ? graphql : rest;
      const member = Reflect.get(active, property, receiver) as unknown;
      return typeof member === 'function' ? (member as (...args: unknown[]) => unknown).bind(active) : member;
    },
  });
}

/**
 * The providers that bind each abstract gateway to its transport-aware router.
 *
 * Components inject only the abstract class. This is the Dependency Inversion Principle doing the
 * real work in this application: it is the single reason the entire feature set can be exercised over
 * two transports without a line of duplicated user interface code.
 */
export const gatewayProviders: readonly Provider[] = [
  {
    provide: AuthGateway,
    useFactory: () =>
      createTransportRouter<AuthGateway>(
        inject(TransportService),
        inject(RestAuthGateway),
        inject(GraphQlAuthGateway),
      ),
  },
  {
    provide: ItemsGateway,
    useFactory: () =>
      createTransportRouter<ItemsGateway>(
        inject(TransportService),
        inject(RestItemsGateway),
        inject(GraphQlItemsGateway),
      ),
  },
  {
    provide: TaxonomyGateway,
    useFactory: () =>
      createTransportRouter<TaxonomyGateway>(
        inject(TransportService),
        inject(RestTaxonomyGateway),
        inject(GraphQlTaxonomyGateway),
      ),
  },
  {
    provide: ExamsGateway,
    useFactory: () =>
      createTransportRouter<ExamsGateway>(
        inject(TransportService),
        inject(RestExamsGateway),
        inject(GraphQlExamsGateway),
      ),
  },
  {
    provide: UsersGateway,
    useFactory: () =>
      createTransportRouter<UsersGateway>(
        inject(TransportService),
        inject(RestUsersGateway),
        inject(GraphQlUsersGateway),
      ),
  },
];
