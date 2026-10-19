export const meta = {
  name: 'verify-research',
  description: 'Answer research questions about Power Platform, then try to refute each answer with two independent checkers',
  whenToUse: 'Before building on a claim about pac, Dataverse or Power Platform behavior. Give questions as a list or a single string (about 3 agents per question). With none given, it checks the first 5 open items in docs/FOUNDATION.md section 8. "confirmed" means two independent checkers failed to refute the answer; it is not proof.',
  phases: [
    { title: 'Questions', detail: 'read the open research items if no questions were given' },
    { title: 'Answer', detail: 'a researcher answers each question with sources' },
    { title: 'Refute', detail: 'two independent researchers try to disprove each answer' },
  ],
}

const AGENT = { agentType: 'powerops-researcher', model: 'sonnet', effort: 'medium' }
const ROLE = 'Follow the instructions in .claude/agents/powerops-researcher.md. Do not edit files.'

const LIST = { type: 'object', properties: { questions: { type: 'array', items: { type: 'string' } } }, required: ['questions'] }
const ANSWER = {
  type: 'object',
  properties: {
    question: { type: 'string' },
    answer: { type: 'string' },
    status: { type: 'string', description: 'GA, preview, deprecated or unverified, with the date' },
    sources: { type: 'array', items: { type: 'string' } },
    confidence: { type: 'string', enum: ['high', 'medium', 'low'] },
  },
  required: ['question', 'answer', 'status', 'sources', 'confidence'],
}
const VERDICT = {
  type: 'object',
  properties: {
    refuted: { type: 'boolean' },
    reason: { type: 'string' },
    sources: { type: 'array', items: { type: 'string' } },
  },
  required: ['refuted', 'reason', 'sources'],
}

// args may arrive as a list, a single string, or a JSON string of a list.
const toList = x => {
  if (typeof x === 'string') {
    try { const j = JSON.parse(x); if (Array.isArray(j)) return j } catch {}
    return x.trim() ? [x] : []
  }
  return Array.isArray(x) ? x : x ? [x] : []
}

const MAX_OPEN = 5
let questions = toList(args).filter(q => typeof q === 'string' && q.trim())
if (!questions.length) {
  log(`No questions given: checking the first ${MAX_OPEN} open items in docs/FOUNDATION.md section 8 (about ${1 + MAX_OPEN * 3} agents). To check your own, give questions, e.g. /verify-research "Does pac solution unpack support YAML?"`)
  phase('Questions')
  const found = await agent(
    `${ROLE} Read docs/FOUNDATION.md section 8. Return the first ${MAX_OPEN} unchecked "- [ ]" items as self-contained research questions. Return only the list.`,
    { ...AGENT, phase: 'Questions', label: 'read open items', schema: LIST },
  )
  questions = (found ? found.questions : []).slice(0, MAX_OPEN)
}
if (!questions.length) {
  log('No questions to check (no open items found in docs/FOUNDATION.md section 8).')
  return []
}

const results = await pipeline(
  questions,
  (q) => agent(
    `${ROLE} Answer this question about Power Platform tooling, with sources and status:\n${q}`,
    { ...AGENT, phase: 'Answer', label: `answer: ${q.slice(0, 40)}`, schema: ANSWER },
  ),
  async (answer, q) => {
    if (!answer) return null
    const checks = await parallel([1, 2].map(n => () => agent(
      `${ROLE} Independently try to REFUTE this answer. Check the sources yourself and look for newer or contradicting ones. If you are unsure, say refuted.\nQuestion: ${q}\nAnswer: ${answer.answer}\nSources: ${answer.sources.join(', ')}`,
      { ...AGENT, phase: 'Refute', label: `refute ${n}: ${q.slice(0, 30)}`, schema: VERDICT },
    )))
    const verdicts = checks.filter(Boolean)
    const objections = verdicts.filter(v => v.refuted).map(v => ({ reason: v.reason, sources: v.sources }))
    return {
      question: q,
      answer: answer.answer,
      status: answer.status,
      confidence: answer.confidence,
      sources: answer.sources,
      // Both checkers must have run and neither may refute. This is not proof.
      confirmed: verdicts.length === 2 && objections.length === 0,
      checkersRan: verdicts.length,
      objections,
    }
  },
)

// pipeline keeps null slots, so a failed question is reported instead of vanishing
return results.map((r, i) => r || { question: questions[i], confirmed: false, checkersRan: 0, objections: [], note: 'researcher failed; not checked' })
