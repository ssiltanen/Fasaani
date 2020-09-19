### 1.2.2

- Add static ToString method for QueryDetails to pretty print query details
- Add function to make default configs using QueryDetails.ToString function

### 1.2.1

- Add more properties to index and indexer CEs.
- Add validate support for index and its indexers.

### 1.2.0

- Add CEs for Index and Indexer to make creating indices and indexers easier
- Add functions to create resources based on Index and Indexer CEs


### 1.1.0

- Add helper functions to create Azure Search clients
- Add synchronous search functions
- Update dependencies, since Ply has been updated with multiple versions
- *BREAKING CHANGE*: Change query log function to take QueryDetails record instead of all the items separately


### 1.0.0

- *BUG FIX*: Geography point in Azure search EDM.GeographyPoint uses "lon lat" ordering instead of "lat lon" so change the ordering used in evaluation
- *BREAKING CHANGE*: Change Unary filter operator from ! to !! to avoid issues with name collisions with computation expressions
- *BREAKING CHANGE*: Release Polygon model to be a sequence of Coordinates instead of hard set 4 Coordinates since Azure Search allows that
- Replace TaskBuilder.fs dependency with Ply to get better performance and to get better stacks on errors
- Implement integration index setup, data insert function, and teardown for each test
- Change test project record property casings to CamelCase
- Rename test project .fs files to be categorize tests between unit test and integration test
- Lose -beta versioning now that we have at least some integration tests


### 0.1.2-beta

- *BREAKING CHANGE*: Remove Infinite, NegativeInfinite and NaN custom values and replace them with F# native infinity, -infinity and nan.
- Added unit tests for OData evaluation
- Added setup for integration tests
- Tweaks to project file