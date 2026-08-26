/**
 * Translates between the C# style names the client models use and the CONSTANT_CASE names Hot
 * Chocolate publishes for enum values.
 *
 * This translation is real work that only the GraphQL transport has to do, and it is deliberately
 * paid inside the GraphQL gateway rather than hidden in a shared helper: the study measures mapping
 * cost as part of the transport that incurs it.
 */

/**
 * Converts a GraphQL enum value to the client's name for it.
 *
 * @param value The CONSTANT_CASE value from the server.
 * @returns The PascalCase name used by the view models.
 */
export function fromGraphQlEnum<T extends string>(value: string): T {
  return value
    .toLowerCase()
    .split('_')
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join('') as T;
}

/**
 * Converts a client enum name to the value the server expects.
 *
 * @param value The PascalCase name used by the view models.
 * @returns The CONSTANT_CASE value the schema declares.
 */
export function toGraphQlEnum(value: string): string {
  return value
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1_$2')
    .toUpperCase();
}

/**
 * Converts a list of client enum names to the values the server expects.
 *
 * @param values The names to convert, if any.
 * @returns The converted values, or `undefined` when nothing was supplied.
 */
export function toGraphQlEnums(values: readonly string[] | undefined): string[] | undefined {
  return values && values.length > 0 ? values.map(toGraphQlEnum) : undefined;
}
