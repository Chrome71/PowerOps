export const meta = {
  name: 'design-panel',
  description: 'Get three independent designs for a problem, then have two reviewers score them',
  whenToUse: 'When a feature or change has more than one reasonable approach. Pass the problem as text. Runs 5 agents and changes no files.',
  phases: [
    { title: 'Design', detail: 'three designs from different angles, in parallel' },
    { title: 'Score', detail: 'two reviewers score all three designs' },
  ],
}

const DESIGN = {
  type: 'object',
  properties: {
    approach: { type: 'string' },
    summary: { type: 'string' },
    steps: { type: 'array', items: { type: 'string' } },
    risks: { type: 'array', items: { type: 'string' } },
    effort: { type: 'string', description: 'Rough estimate, e.g. "half a day" or "about 3 PRs"' },
  },
  required: ['approach', 'summary', 'steps', 'risks', 'effort'],
}
const SCORES = {
  type: 'object',
  properties: {
    scores: {
      type: 'array',
      items: {
        type: 'object',
        properties: { id: { type: 'string', enum: ['A', 'B', 'C'] }, score: { type: 'number', description: '1 to 10' }, rationale: { type: 'string' } },
        required: ['id', 'score', 'rationale'],
      },
    },
    recommended: { type: 'string', enum: ['A', 'B', 'C'] },
  },
  required: ['scores', 'recommended'],
}

// args may arrive as text, a list of strings, or a JSON string of a list.
const toList = x => {
  if (typeof x === 'string') {
    try { const j = JSON.parse(x); if (Array.isArray(j)) return j } catch {}
    return x.trim() ? [x] : []
  }
  return Array.isArray(x) ? x : x ? [x] : []
}

const problem = toList(args).filter(x => typeof x === 'string').join('\n').trim()
if (!problem) {
  log('Usage: /design-panel <problem statement>. Example: /design-panel add a "powerops diff" command for two solutions. Nothing to do.')
  return { error: 'missing problem statement' }
}

phase('Design')
const ANGLES = [
  'Smallest useful version first: the least that solves the core of the problem, and what to defer.',
  'Risk first: find the riskiest assumptions and unknowns, and design to retire them early.',
  'Contributor and user experience first: how it feels to use and to contribute to, with the simplest interface.',
]
const designs = (await parallel(ANGLES.map((angle, i) => () => agent(
  `Follow the instructions in .claude/agents/powerops-designer.md. Design only: do NOT edit files or change git state. Read AGENTS.md and docs/FOUNDATION.md, then design a solution.\nProblem: ${problem}\nAngle: ${angle}`,
  { agentType: 'powerops-designer', model: 'sonnet', effort: 'high', phase: 'Design', label: `design ${i + 1}`, schema: DESIGN },
).then(d => d && { ...d, id: 'ABC'[i] })))).filter(Boolean)

if (designs.length < 2) {
  log(`Only ${designs.length} design(s) came back, so there is nothing to compare.`)
  return { problem, ranking: [], designs, reviews: [] }
}

phase('Score')
const reviews = (await parallel([1, 2].map(n => () => agent(
  `Follow the instructions in .claude/agents/powerops-reviewer.md. Do not edit files. Score each design (by its id) from 1 to 10 against the problem and the rules in AGENTS.md. Give a short rationale per design, then name the id you recommend.\nProblem: ${problem}\nDesigns:\n${JSON.stringify(designs, null, 2)}`,
  { agentType: 'powerops-reviewer', model: 'sonnet', effort: 'high', phase: 'Score', label: `score ${n}`, schema: SCORES },
)))).filter(Boolean)
if (!reviews.length) log('Both scorers failed; the designs are returned unscored.')

const ranking = designs.map(d => {
  const s = reviews.flatMap(r => r.scores.filter(x => x.id === d.id).map(x => x.score))
  return {
    id: d.id,
    approach: d.approach,
    average: s.length ? Math.round(10 * s.reduce((a, b) => a + b, 0) / s.length) / 10 : null,
    recommendedBy: reviews.filter(r => r.recommended === d.id).length,
  }
}).sort((a, b) => (b.average ?? -1) - (a.average ?? -1))

return { problem, ranking, designs, reviews }
