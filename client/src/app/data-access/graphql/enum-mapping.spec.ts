import { fromGraphQlEnum, toGraphQlEnum, toGraphQlEnums } from './enum-mapping';

/**
 * The GraphQL schema publishes enum values in CONSTANT_CASE while the client models use the C# style
 * names. That translation is real work only the GraphQL transport has to do, so it is pinned down
 * here rather than assumed.
 */
describe('GraphQL enum mapping', () => {
  it.each([
    ['MULTIPLE_CHOICE_SINGLE_RESPONSE', 'MultipleChoiceSingleResponse'],
    ['EITHER_OR', 'EitherOr'],
    ['ESSAY', 'Essay'],
    ['IN_REVIEW', 'InReview'],
    ['VERY_HARD', 'VeryHard'],
  ])('reads %s as %s', (wire, client) => {
    expect(fromGraphQlEnum(wire)).toBe(client);
  });

  it.each([
    ['MultipleChoiceMultipleResponse', 'MULTIPLE_CHOICE_MULTIPLE_RESPONSE'],
    ['EitherOr', 'EITHER_OR'],
    ['Draft', 'DRAFT'],
    ['VeryEasy', 'VERY_EASY'],
  ])('writes %s as %s', (client, wire) => {
    expect(toGraphQlEnum(client)).toBe(wire);
  });

  it('round-trips every value it converts', () => {
    const values = ['MultipleChoiceSingleResponse', 'InReview', 'VeryHard', 'Archived'];

    expect(values.map((value) => fromGraphQlEnum(toGraphQlEnum(value)))).toEqual(values);
  });

  it('omits an empty list rather than sending one', () => {
    expect(toGraphQlEnums(undefined)).toBeUndefined();
    expect(toGraphQlEnums([])).toBeUndefined();
    expect(toGraphQlEnums(['Draft', 'Published'])).toEqual(['DRAFT', 'PUBLISHED']);
  });
});
