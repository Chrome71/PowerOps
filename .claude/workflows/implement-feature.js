export const meta = {
  name: 'implement-feature',
  description: 'Implement tasks one at a time on your current branch, one commit each, with a review of each commit before the next task starts',
  whenToUse: 'When you have one or more small, well-scoped tasks. Give the tasks as a list (strings or {title, details}) or one task as text. Needs a clean git working tree. Commits locally, never pushes, stops at the first task that fails review. 2 agents per task, at most 5 tasks.',
  phases: [
    { title: 'Implement', detail: 'an implementer makes one commit for the task, with build, tests and format passing' },
    { title: 'Review', detail: 'a reviewer reads that commit and runs build and tests' },
  ],
}

const BUILD = {
  type: 'object',
  properties: {
    status: { type: 'string', enum: ['committed', 'blocked'], description: 'blocked = dirty tree, unclear task, or checks could not be made to pass; nothing committed' },
    sha: { type: 'string', description: 'Full SHA from git rev-parse HEAD; empty when blocked' },
    branch: { type: 'string' },
    summary: { type: 'string' },
    checksPassed: { type: 'boolean', description: 'build, test and format check all passed' },
    notes: { type: 'string' },
  },
  required: ['status', 'sha', 'branch', 'summary', 'checksPassed', 'notes'],
}
const REVIEW = {
  type: 'object',
  properties: {
    verdict: { type: 'string', enum: ['approve', 'changes'] },
    findings: { type: 'array', items: { type: 'string' } },
  },
  required: ['verdict', 'findings'],
}

// args may arrive as a list, a single task (text or {title, details}), or a JSON string of a list.
const toList = x => {
  if (typeof x === 'string') {
    try { const j = JSON.parse(x); if (Array.isArray(j)) return j } catch {}
    return x.trim() ? [x] : []
  }
  return Array.isArray(x) ? x : x ? [x] : []
}

const MAX = 5
let tasks = toList(args).filter(t => typeof t === 'string' ? t.trim() : t && t.title)
if (!tasks.length) {
  log('Usage: /implement-feature <task>. Example: /implement-feature add a --json flag to the list command. Several tasks: give a list; they run in order on your current branch.')
  return []
}
if (tasks.length > MAX) {
  log(`Running the first ${MAX} of ${tasks.length} tasks. Run again for the rest.`)
  tasks = tasks.slice(0, MAX)
}
const text = t => typeof t === 'string' ? t : `${t.title}${t.details ? '\n' + t.details : ''}`
const name = t => (typeof t === 'string' ? t : t.title).slice(0, 40)

const results = []
for (const [i, t] of tasks.entries()) {
  const n = `${i + 1}/${tasks.length}`
  const built = await agent(
    `Follow the instructions in .claude/agents/powerops-implementer.md.
First run \`git status --porcelain\`. If it prints anything, stop: status "blocked", note "working tree not clean", change nothing.
If the current branch is main or master, or HEAD is detached, first create and switch to a branch feat/<short-slug>.
Then implement the task: test first, then build, test and \`dotnet format --verify-no-changes\` must all pass. Make exactly ONE commit with a Conventional Commit message, containing only this task. Never push. If you cannot get all checks to pass, do not commit: status "blocked" and say why. Report the full SHA from \`git rev-parse HEAD\`.
Task: ${text(t)}`,
    { agentType: 'powerops-implementer', model: 'sonnet', effort: 'high', phase: 'Implement', label: `implement ${n}: ${name(t)}`, schema: BUILD },
  ).catch(() => null)

  if (!built || built.status !== 'committed' || !built.checksPassed || !/^[0-9a-f]{7,40}$/.test(built.sha)) {
    results.push({ task: text(t), status: 'blocked', notes: built ? built.notes : 'implementer failed' })
    break
  }
  const review = await agent(
    `Follow the instructions in .claude/agents/powerops-reviewer.md. Do not edit files.
Review commit ${built.sha} with \`git show ${built.sha}\` against the task. Run \`dotnet build\` and \`dotnet test\` (HEAD should be ${built.sha}; if \`git rev-parse HEAD\` differs, say so).
Task: ${text(t)}
Approve only if the build and tests pass and you find no high or medium problems.`,
    { agentType: 'powerops-reviewer', model: 'sonnet', effort: 'high', phase: 'Review', label: `review ${n}: ${name(t)}`, schema: REVIEW },
  ).catch(() => null)

  const ok = review && review.verdict === 'approve'
  results.push({
    task: text(t),
    status: ok ? 'approved' : review ? 'needs-changes' : 'review-failed',
    sha: built.sha,
    branch: built.branch,
    summary: built.summary,
    findings: review ? review.findings : [],
  })
  if (!ok) break
}
for (const t of tasks.slice(results.length)) results.push({ task: text(t), status: 'skipped', notes: 'an earlier task did not pass' })
return results
