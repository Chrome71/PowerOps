export const meta = {
  name: 'review-pr',
  description: 'Review the current branch from three angles, then try to refute every finding and keep the survivors',
  whenToUse: 'Before opening a pull request. Reviews committed and uncommitted changes on your current branch against a base ref (default origin/main; pass e.g. "main" to change it). 3 reviewers plus one checker per finding, at most 18 agents.',
  phases: [
    { title: 'Review', detail: 'correctness, security and simplicity reviewers read the diff' },
    { title: 'Refute', detail: 'a reviewer tries to disprove each finding' },
  ],
}

const FINDINGS = {
  type: 'object',
  properties: {
    findings: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          file: { type: 'string' },
          line: { type: 'number' },
          severity: { type: 'string', enum: ['high', 'medium', 'low'] },
          summary: { type: 'string' },
          failureScenario: { type: 'string', description: 'What breaks, and how' },
        },
        required: ['file', 'line', 'severity', 'summary', 'failureScenario'],
      },
    },
    filesReviewed: { type: 'number', description: 'Number of changed files you looked at; 0 if the diff was empty or git failed' },
    baseUsed: { type: 'string', description: 'The base ref you actually diffed against' },
  },
  required: ['findings', 'filesReviewed', 'baseUsed'],
}
const VERDICT = {
  type: 'object',
  properties: { refuted: { type: 'boolean' }, reason: { type: 'string' } },
  required: ['refuted', 'reason'],
}

const base = typeof args === 'string' && args.trim() ? args.trim() : 'origin/main'
if (args !== undefined && typeof args !== 'string') log('Usage: /review-pr [base-ref], e.g. /review-pr main. Using origin/main.')

const LENSES = [
  ['correctness', 'bugs, missed edge cases, wrong exit codes, broken tests'],
  ['security', 'secrets or tenant URLs, injection into pac or shell arguments, unsafe writes to a tenant or to files'],
  ['simplicity', 'code, options, abstractions or dependencies that are not needed, and broken rules in AGENTS.md'],
]
const SEV = { high: 0, medium: 1, low: 2 }
const MAX_PER_LENS = 5
const ROLE = 'Follow the instructions in .claude/agents/powerops-reviewer.md. Do not edit files.'
log(`Reviewing the current branch against ${base}. Up to ${3 + 3 * MAX_PER_LENS} agents. To use another base: /review-pr main`)

const lensResults = await pipeline(
  LENSES,
  ([lens, focus]) => agent(
    `${ROLE}
Review the current branch through the ${lens} lens only: ${focus}.
Get the change with \`git diff ${base}...HEAD\`. If \`${base}\` does not exist, try origin/main, then main, and report which you used in baseUsed. Also check \`git diff HEAD\` and \`git status --short\`, and include uncommitted changes in your review.
Report at most ${MAX_PER_LENS} real problems, most severe first. Set filesReviewed to 0 if the diff was empty or git failed.`,
    { agentType: 'powerops-reviewer', model: 'sonnet', effort: 'high', phase: 'Review', label: `review: ${lens}`, schema: FINDINGS },
  ),
  async (found, [lens]) => {
    const top = [...found.findings].sort((a, b) => SEV[a.severity] - SEV[b.severity]).slice(0, MAX_PER_LENS)
    const checked = await parallel(top.map(f => () => agent(
      `${ROLE} Try to REFUTE this finding in the change on the current branch (\`git diff ${found.baseUsed}...HEAD\`, plus uncommitted changes). Read the code. Refute it if the failure cannot actually happen.\n${JSON.stringify(f)}`,
      { agentType: 'powerops-reviewer', model: 'sonnet', effort: 'high', phase: 'Refute', label: `refute: ${lens} ${f.file}:${f.line}`, schema: VERDICT },
    ).catch(() => null).then(v => ({ ...f, lens, unverified: !v, refuted: v ? v.refuted : false, refutation: v ? v.reason : 'refuter failed, finding kept' }))))
    return { lens, total: found.findings.length, filesReviewed: found.filesReviewed, baseUsed: found.baseUsed, checked: checked.filter(Boolean) }
  },
)

const all = lensResults.filter(Boolean)
const kept = all.flatMap(r => r.checked.filter(f => !f.refuted)).sort((a, b) => SEV[a.severity] - SEV[b.severity])
const warnings = [
  ...LENSES.map(l => l[0]).filter(l => !all.some(r => r.lens === l)).map(l => `The ${l} reviewer failed, so that lens was not checked.`),
  ...all.filter(r => r.filesReviewed === 0).map(r => `The ${r.lens} reviewer saw an empty diff (wrong base ref, or nothing changed?), so "no findings" means nothing.`),
  ...(kept.some(f => f.unverified) ? ['Some findings could not be checked by a refuter and are marked unverified.'] : []),
]
return {
  base,
  baseUsed: all[0] && all[0].baseUsed,
  warnings,
  findings: kept,
  refutedCount: all.reduce((n, r) => n + r.checked.filter(f => f.refuted).length, 0),
  byLens: all.map(r => ({ lens: r.lens, found: r.total, kept: r.checked.filter(f => !f.refuted).length })),
}
