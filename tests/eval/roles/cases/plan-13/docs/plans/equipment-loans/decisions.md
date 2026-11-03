# Decisions: equipment-loans

## Round 1

### Q1. Who uses the loans app?
1. Store keepers at the counter, staff for their own loans, and team leads for overdue loans (recommended)
2. Store keepers only
3. In your own words
Answer: Store keepers at the counter, staff for their own loans, and team leads for overdue loans

### Q2. Where does the equipment data live today?
1. In the store keepers' Equipment list on the depots site: extend it and connect to it (recommended)
2. Copy it into new tables
3. In your own words
Answer: In the store keepers' Equipment list on the depots site: extend it and connect to it

### Q3. Licensing?
1. Microsoft 365 E3 for every user, standard connectors only, and no Managed Environment (recommended)
2. Power Apps per-user plans and Dataverse
3. In your own words
Answer: Microsoft 365 E3 for every user, standard connectors only, and no Managed Environment

## Round 2

### Q4. How many loans, now and in three years, and what is the widest filter?
1. About 1,100 a year, kept three years after return, so about 3,300; the widest filter is all open overdue loans (recommended)
2. Keep loans forever
3. In your own words
Answer: About 275 a year at each depot, kept three years after return; each depot keeps its own list, and the widest filter is one depot's open overdue loans

### Q5. Who sees which loans?
1. All staff may see all loans, which are Internal; only store keepers change them (recommended)
2. Each team sees only its own loans
3. In your own words
Answer: All staff may see all loans, which are Internal; only store keepers change them

### Q6. Which environments does the solution move through?
1. Dev, Test and Prod, managed in Test and Prod, through the Azure DevOps pipeline (recommended)
2. Dev and Prod only
3. In your own words
Answer: Dev, Test and Prod, managed in Test and Prod, through the Azure DevOps pipeline

## Round 3

### Q7. What is out of this version?
1. Advance booking, a damage report workflow, and barcode labels (recommended)
2. Nothing
3. In your own words
Answer: Nothing

### Q8. Is offline needed?
1. No, every depot has Wi-Fi (recommended)
2. Yes, for the store keepers
3. In your own words
Answer: No, every depot has Wi-Fi

### Q9. Which rules apply?
1. The council's information security standard and the tenant data policy as it stands (recommended)
2. Only the tenant defaults
3. In your own words
Answer: The council's information security standard and the tenant data policy as it stands

## Log

- 2026-10-27: plan started at the request of the facilities manager
- depth: standard
- approved 2026-10-29 by the requester, with all five areas asked
