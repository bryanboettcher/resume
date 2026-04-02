---
title: Angular Service Patterns — Strategy Pattern for String Matching
tags: [angular, typescript, strategy-pattern, levenshtein, fuzzy-matching, interface-design, csv-import]
related:
  - evidence/angular-service-patterns.md
  - evidence/angular-service-patterns-caching-dropdown.md
  - evidence/angular-service-patterns-observable-composition.md
  - evidence/frontend-web-development.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/angular-service-patterns.md
---

# Angular Service Patterns — Strategy Pattern for String Matching

The Madera Angular CSV import feature uses an `IMatchingService` interface with a `LevenshteinDistanceMatchingService` implementation to fuzzy-match uploaded CSV headers against expected field names. The dynamic programming Levenshtein distance algorithm returns a match only when the edit distance is under 50% of the word length — providing auto-suggested column mappings while avoiding false positives. The strategy pattern allows substituting a different algorithm (e.g., Jaro-Winkler) without changing the import component.

---

## Evidence: Strategy Pattern for String Matching

**Files:**
- `Madera/madera.ui.client/src/app/services/matching-service.ts`
- `Madera/madera.ui.client/src/app/services/levenshtein-distance-matching.service.ts`

The string matching subsystem uses an interface-based strategy pattern. The `IMatchingService` interface defines a single `findMatch(word, choices)` method:

```typescript
export interface IMatchingService {
  findMatch(word: string, choices: string[]): WordMatch | undefined;
}
export class WordMatch {
  public choice: string = '';
  public distance: number = 2147483647; // something absurd that will never be considered a good match
}
```

The `LevenshteinDistanceMatchingService` implements this interface with a full dynamic programming Levenshtein distance algorithm. It computes edit distance between the input word and every choice (case-insensitive), sorts by distance, and returns the best match only if the distance is under 50% of the word length:

```typescript
return bestMatch.distance < (word.length * 0.5)
  ? new WordMatch({ choice: bestMatch.choice, distance: bestMatch.distance })
  : undefined;
```

This service powers the CSV import column mapping feature, where uploaded CSV headers are fuzzy-matched against expected field names to suggest mappings automatically. The strategy pattern means a different matching algorithm (e.g., Jaro-Winkler) could be substituted without changing the import component.

## Key Files

- `madera-apps:Madera/madera.ui.client/src/app/services/levenshtein-distance-matching.service.ts` — Dynamic programming fuzzy matcher
- `madera-apps:Madera/madera.ui.client/src/app/services/matching-service.ts` — Strategy interface for matching
