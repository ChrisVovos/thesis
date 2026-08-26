import { TestBed } from '@angular/core/testing';
import { TransportService } from './transport.service';

// The default environment is the production one, which deliberately pins the transport and ignores
// storage. These tests are about the behaviour a developer and the benchmark build see, so the
// environment is replaced with the non-production settings.
jest.mock('../../../environments/environment', () => ({
  environment: {
    production: false,
    defaultTransport: 'rest',
    showTransportSelector: true,
    restBaseUrl: '/api/v1',
    graphqlUrl: '/graphql',
    collectMetrics: true,
  },
}));

describe('TransportService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.resetTestingModule();
  });

  function create(): TransportService {
    TestBed.configureTestingModule({});
    return TestBed.inject(TransportService);
  }

  it('falls back to the configured default when nothing is stored', () => {
    expect(create().active()).toBe('rest');
  });

  it('restores a previously chosen transport', () => {
    localStorage.setItem(TransportService.StorageKey, 'graphql');

    expect(create().active()).toBe('graphql');
  });

  it('discards a stored value that does not name a supported transport', () => {
    localStorage.setItem(TransportService.StorageKey, 'carrier-pigeon');

    const service = create();

    expect(service.active()).toBe('rest');
    expect(localStorage.getItem(TransportService.StorageKey)).toBeNull();
  });

  it('persists the choice so it survives a reload', () => {
    const service = create();

    service.use('graphql');

    expect(service.active()).toBe('graphql');
    expect(localStorage.getItem(TransportService.StorageKey)).toBe('graphql');
  });

  it('ignores a switch to the transport that is already active', () => {
    const service = create();
    const info = jest.spyOn(console, 'info').mockImplementation(() => undefined);

    service.use('rest');

    expect(info).not.toHaveBeenCalled();
    info.mockRestore();
  });

  it('records every change so a benchmark run is traceable', () => {
    const service = create();
    const info = jest.spyOn(console, 'info').mockImplementation(() => undefined);

    service.use('graphql');

    expect(info).toHaveBeenCalledWith(
      '[transport] switched',
      expect.objectContaining({ from: 'rest', to: 'graphql' }),
    );
    info.mockRestore();
  });
});
