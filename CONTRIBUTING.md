# Contributing to TerminNaKlik (TNK)

First off, thank you for considering contributing to TerminNaKlik! We welcome any help to make this project better. Whether it's reporting a bug, discussing improvements, or submitting a pull request, your contribution is valuable.

Please take a moment to review this document in order to make the contribution process easy and effective for everyone involved.

## Table of Contents

* [Code of Conduct](#code-of-conduct)
* [Getting Started](#getting-started)
    * [Prerequisites](#prerequisites)
    * [Fork & Clone](#fork--clone)
    * [Setting Up Your Environment](#setting-up-your-environment)
* [How Can I Contribute?](#how-can-i-contribute)
    * [Reporting Bugs](#reporting-bugs)
    * [Suggesting Enhancements](#suggesting-enhancements)
    * [Your First Code Contribution](#your-first-code-contribution)
    * [Pull Requests](#pull-requests)
* [Development Workflow](#development-workflow)
    * [Branching](#branching)
    * [Committing](#committing)
    * [Coding Style](#coding-style)
    * [Testing](#testing)
* [Issue and Pull Request Labels](#issue-and-pull-request-labels)

## Code of Conduct

This project and everyone participating in it is governed by the [TerminNaKlik Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior to me.

## Getting Started

### Prerequisites

Ensure you have the following installed:
* Git
* .NET SDK (version specified in `global.json`, e.g., 9.0.100-rc.2.24474.11)
* Node.js and npm (latest LTS version)
* Angular CLI (version compatible with the project, e.g., v19)
* PostgreSQL Server
* A suitable IDE (e.g., Visual Studio 2022 for backend, VS Code for frontend)

Refer to the main [README.md](README.md) for detailed setup instructions.

### Fork & Clone

1.  **Fork** the repository on GitHub.
2.  **Clone** your fork locally:
    ```bash
    git clone [https://github.com/isql/TNK.git](https://github.com/isql/TNK.git)
    cd TNK
    ```
3.  **Add an upstream remote** to keep your fork in sync with the main repository:
    ```bash
    git remote add upstream [https://github.com/isql/TNK.git](https://github.com/isql/TNK.git)
    ```

### Setting Up Your Environment

Follow the "Getting Started" instructions in the main [README.md](README.md) to set up the backend and frontend development environments. This includes database setup, configuration, and installing dependencies.

## How Can I Contribute?

### Reporting Bugs

If you find a bug, please ensure the bug was not already reported by searching on GitHub under [Issues](https://github.com/isql/TNK/issues).

If you're unable to find an open issue addressing the problem, [open a new one](https://github.com/isql/TNK/issues/new). Be sure to include a **title and clear description**, as much relevant information as possible, and a **code sample or an executable test case** demonstrating the expected behavior that is not occurring.

Provide information about your environment (e.g., OS, .NET version, Node version, browser version).

### Suggesting Enhancements

If you have an idea for an enhancement, we'd love to hear about it! Please [open an issue](https://github.com/isql/TNK/issues/new) and provide:
* A **clear and descriptive title**.
* A **detailed explanation** of the enhancement: what it is, why it's needed, and how it would work.
* **Use-cases** that this enhancement would address.
* **Potential drawbacks or considerations**.

This allows for discussion and refinement of the idea before any code is written.

### Your First Code Contribution

Unsure where to begin contributing to TNK? You can start by looking through these `good first issue` and `help wanted` issues:
* [Good first issues](https://github.com/isql/TNK/labels/good%20first%20issue) - issues which should only require a few lines of code, and a test or two.
* [Help wanted issues](https://github.com/isql/TNK/labels/help%20wanted) - issues which should be a bit more involved than `good first issue` issues.

### Pull Requests

When you're ready to contribute code:
1.  Ensure any install or build dependencies are removed before the end of the layer when doing a build.
2.  Create a new branch from `main` (or the relevant feature/develop branch) for your changes. See [Branching](#branching).
3.  Make your changes, adhering to the [Coding Style](#coding-style).
4.  Add or update tests as appropriate. See [Testing](#testing).
5.  Ensure all tests pass.
6.  Commit your changes with a descriptive commit message. See [Committing](#committing).
7.  Push your branch to your fork on GitHub.
8.  Open a pull request (PR) to the `main` branch of the original TNK repository.
    * Provide a clear title and description for your PR, explaining the "what" and "why" of your changes.
    * Reference any related issues (e.g., "Fixes #123" or "Closes #123").
    * Ensure your PR passes all automated checks (CI builds, tests, linting).
    * Be prepared to discuss your changes and make adjustments if requested by reviewers.

## Development Workflow

### Branching

* Create new branches from the `main` branch (or `develop` if that's the primary development branch).
* Use descriptive branch names, prefixed appropriately:
    * `feature/your-feature-name` (e.g., `feature/user-profile-editing`)
    * `fix/issue-description` (e.g., `fix/login-button-alignment`)
    * `docs/topic-update` (e.g., `docs/readme-setup-guide`)
    * `refactor/area-being-refactored` (e.g., `refactor/auth-service`)

### Committing

* Write clear, concise, and descriptive commit messages.
* Follow the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification if possible. This helps in generating changelogs and understanding the history.
    * Example: `feat: Add user avatar upload functionality`
    * Example: `fix: Correct calculation error in booking price`
    * Example: `docs: Update contribution guidelines`
* Commit logical units of work. Avoid large, monolithic commits.

### Coding Style

#### Backend (.NET / C#)
* Follow the established coding conventions in the project (generally Microsoft's C# Coding Conventions).
* Use meaningful names for variables, methods, and classes.
* Write clean, readable, and maintainable code.
* Comment code where necessary to explain complex logic, but prefer self-documenting code.
* Ensure proper using statement organization and remove unused usings.

#### Frontend (Angular / TypeScript / SCSS / Syncfusion)
* Follow the [Angular Style Guide](https://angular.io/guide/styleguide).
* Follow the [Angular Syncfusion Docs](https://helpej2.syncfusion.com/angular/documentation/).
* Use Prettier and ESLint to maintain consistent code formatting and quality.
* Organize SCSS using a modular approach.
* Ensure components are well-encapsulated and reusable where appropriate.

### Testing

#### Backend
* **Unit Tests:** Write unit tests for new business logic, services, and CQRS handlers. Place them in the `TNK.UnitTests` project.
* **Integration Tests:** For interactions with external dependencies like the database, write integration tests. Place them in `TNK.IntegrationTests`.
* **Functional/API Tests:** For testing API endpoints, write tests in `TNK.FunctionalTests`.
* Aim for good test coverage. All new features and bug fixes should ideally be accompanied by tests.

#### Frontend
* **Unit Tests:** Write unit tests for components, services, pipes, and guards using Karma and Jasmine.
* **End-to-End (E2E) Tests:** Consider adding E2E tests for critical user flows using a framework like Cypress or Playwright (if not already set up).
* Ensure all tests pass before submitting a PR: `ng test`.

## Issue and Pull Request Labels

We use labels to organize issues and pull requests. Some common labels include:
* `bug`: A confirmed bug.
* `enhancement`: A feature request or improvement.
* `documentation`: Related to documentation changes.
* `good first issue`: Suitable for new contributors.
* `help wanted`: Needs attention or assistance.
* `in progress`: Actively being worked on.
* `needs review`: Pull request is ready for review.
* `backend`: Related to the .NET backend.
* `frontend`: Related to the Angular frontend.

---

Thank you for contributing to TerminNaKlik! Your efforts help make this project better for everyone.
