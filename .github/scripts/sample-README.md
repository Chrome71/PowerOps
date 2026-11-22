# PowerOps sample

A small Power Platform solution to try PowerOps on when you have no export of your own. It was made for the PowerOps project (Apache-2.0) and holds only fake values.

- `solution/`: the unpacked solution `PowerOpsSeed`: a table, an environment variable, a connection reference, the cloud flow "Stamp work item status" and a canvas app (its package is left out). Two faults are in it on purpose.
- `tests/`: two flow tests for that flow, which `powerops test` runs offline.

Run these in this folder (on Windows, `powerops` is `powerops-win-x64.exe` unless you renamed it):

```sh
powerops analyze solution
powerops test solution --tests tests
```

Expected result:

- `analyze` exits 0 and reports the two faults as warnings: PO005, the connection reference `pops_SpareDataverse` that nothing uses, and PO006, the SharePoint URL the flow hard-codes. A note says the canvas app was not checked because its package is not in the folder. Its last line is `1 solution, 0 errors, 2 warnings`.
- `test` exits 0. Its last line is `2 tests: 2 passed, 0 failed, 0 unmocked, 0 unsupported`.
