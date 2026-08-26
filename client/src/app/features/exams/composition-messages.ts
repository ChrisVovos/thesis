/**
 * Turns the composition rule codes the server publishes into sentences a user can act on.
 *
 * The codes are identical over REST and GraphQL — they come from the exam aggregate, not from a
 * transport — so this one table serves both surfaces.
 *
 * @param code The rule code reported by the server.
 * @returns The message to show.
 */
export function describeCompositionViolation(code: string): string {
  switch (code) {
    case 'exam.no_sections':
      return 'Add at least one section before publishing.';
    case 'exam.empty_section':
      return 'Every section must contain at least one item.';
    case 'exam.duplicate_item':
      return 'The same item appears more than once in this exam.';
    default:
      return code;
  }
}
