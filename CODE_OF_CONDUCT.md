# Code of Conduct

Welcome to our Grasshopper development repository!

To ensure smooth collaboration and a helpful environment for everyone, please follow these guidelines:

## General Conduct

- Be respectful and constructive in discussions and pull requests.
- Keep the repository organized and avoid committing unnecessary files.

## Bug Reports

- If you encounter a bug or unexpected behavior, **open a GitHub Issue**.
- Include a **clear description** of the issue.
- Add a **screenshot** to help illustrate the problem.
- Whenever possible, upload the **Rhino (.3dm) and Grasshopper (.gh/.ghx) files** that reproduce the issue.

## Feature Requests

- If you have an idea for a feature or you feel that something is missing, **open a GitHub Issue**.
- Include a **clear description** of the proposed feature.
- Ideally, add **references** and/or **examples** to enable us understand your proposal
- If possible, upload the **Rhino (.3dm) and Grasshopper (.gh/.ghx) files** that prototype the feature in a test-case

## Development

### Development Files

- Development Grasshopper files are in `grasshopper_development`

### Release Action

- When a release is created and pushed to the `grasshopper_release/` folder, a **GitHub release will be automatically generated** via GitHub Actions.
- Please follow the naming and structure conventions in the `grasshopper_release/` folder to ensure automation runs smoothly:
- The **Rhino** file should be named: *YYMMDD_DDU_Clay3DPrinting_Slicer_RELEASE.3dm*
- The **Grasshopper** file should be named: *YYMMDD_DDU_Clay3DPrinting_Slicer_RELEASE.gh*

Thank you for contributing!
