import { render, screen } from '@testing-library/angular';
import userEvent from '@testing-library/user-event';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { TransportService } from '../../../core/transport/transport.service';
import { TransportSelector } from './transport-selector';

// The selector is hidden in production builds, so the tests run against the settings a developer and
// the benchmark build see.
jest.mock('../../../../environments/environment', () => ({
  environment: {
    production: false,
    defaultTransport: 'rest',
    showTransportSelector: true,
    restBaseUrl: '/api/v1',
    graphqlUrl: '/graphql',
    collectMetrics: true,
  },
}));

describe('TransportSelector', () => {
  beforeEach(() => localStorage.clear());

  async function setup() {
    const view = await render(TransportSelector, {
      providers: [provideAnimationsAsync('noop')],
    });
    return { view, transport: view.fixture.debugElement.injector.get(TransportService) };
  }

  it('is discoverable by the identifier the end-to-end suite drives', async () => {
    await setup();

    expect(screen.getByTestId('transport-selector')).toBeInTheDocument();
  });

  it('reflects the currently active transport', async () => {
    const { transport, view } = await setup();

    transport.use('graphql');
    await view.fixture.whenStable();
    view.detectChanges();

    expect(screen.getByTestId('transport-selector')).toHaveTextContent('GraphQL');
  });

  it('offers both transports and switches to the chosen one', async () => {
    const { transport } = await setup();

    await userEvent.click(screen.getByTestId('transport-selector'));
    const graphql = await screen.findByText('GraphQL');
    await userEvent.click(graphql);

    expect(transport.active()).toBe('graphql');
  });

  it('announces the change to assistive technology', async () => {
    const { transport, view } = await setup();

    transport.use('graphql');
    await view.fixture.whenStable();
    view.detectChanges();

    const status = screen.getByTestId('transport-status');
    expect(status).toHaveAttribute('aria-live', 'polite');
    expect(status).toHaveTextContent('API transport is now GraphQL.');
  });
});
