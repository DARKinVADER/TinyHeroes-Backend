# TinyHeroes Testing Guide

This guide is for developers and details the various types of tests in the TinyHeroes repository, how to run them, and how mocking strategies are structured.

## Table of Contents
1. [Backend Unit & Integration Tests](#backend-unit--integration-tests)
2. [Frontend Unit Tests](#frontend-unit-tests)
3. [Frontend E2E Tests (Playwright)](#frontend-e2e-tests-playwright)

---

## Backend Unit & Integration Tests

Backend tests are written using **xUnit** and generally test application logic, infrastructure code, and API endpoints.

- **Location**: `backend/TinyHeroes.Tests`
- **Framework**: xUnit
- **Mocking**: 
  - Uses [NSubstitute](https://nsubstitute.github.io/) to mock external dependencies (e.g., databases, external API clients).
  - Integration tests use WebApplicationFactory to spin up an in-memory API and test endpoints end-to-end (without a real database, often using SQLite or a mock database context depending on the test scope).

### How to Run

From the root directory or `backend` folder:
```bash
# Run all tests
dotnet test backend/TinyHeroes.slnx

# Run tests with code coverage (outputs to TestResults)
dotnet test backend/TinyHeroes.slnx --collect:"XPlat Code Coverage"
```

---

## Frontend Unit Tests

Frontend tests run via **Vitest** for Angular components, services, and pipes. 

- **Location**: `frontend/src/**/*.spec.ts`
- **Framework**: Vitest (replacing Jasmine/Karma)
- **Mocking**: 
  - Vitest provides `vi.fn()` and `vi.spyOn()` for function mocking.
  - Mocking Angular HTTP is typically done using `HttpTestingController` from `@angular/common/http/testing`.
  - Component dependencies are mocked using standard Angular testing utilities (providing mock classes via the `TestBed`).

### How to Run

From the `frontend` directory:
```bash
# Run tests
npm run test

# Run tests with code coverage
npm run test:coverage
```

---

## Frontend E2E Tests (Playwright)

End-to-End tests simulate a real user's browser interactions. Playwright is used to ensure the most critical user journeys work as expected.

- **Location**: `frontend/e2e/`
- **Framework**: Playwright
- **Helpers**: Shared utility functions and mocked API responses are in `frontend/e2e/helpers/`.

### Mocking Strategy

The Playwright tests can run in two distinct modes: **Mock Mode** and **Integration Mode**.

#### 1. Mock Mode (Default Local Dev)
By default, tests run locally with mocked backend APIs (`isMockMode() === true`). 
- **Setup**: In `base-test.ts`, tests selectively call mock setups like `mockAuthApi(page)` or `mockAllApi(page)`. 
- **Why**: Allows fast, reliable testing without needing a database or real backend running.
- **Turnstile**: The Cloudflare Turnstile CAPTCHA is automatically bypassed. We inject a mock global turnstile object in `base-test.ts` so login and registration buttons are enabled immediately.

#### 2. Integration Mode (CI & Live Testing)
When the tests target a live environment (e.g. staging or integration), **all network mocking is disabled**.
- **Setup**: Triggered when `PLAYWRIGHT_BASE_URL` is set.
- **Behavior**: `base-test.ts` disables `page.route` intercepts, forcing the tests to communicate with the real `E2E_API_BASE_URL`.
- **Note**: Some tests skip themselves if `isMockMode() === false` because they rely on injecting explicit database states that aren't possible on a real environment.

### How to Run

**1. Local (Mock Mode)**
Runs tests against `http://localhost:4200`. Uses mocked APIs.
```bash
npm run e2e
npm run e2e:ui # Opens Playwright UI
```

**2. Against Integration Environment (Live Backend)**
Runs tests locally but connects to the deployed integration environment (skipping mock APIs). Useful for debugging real-world failures.

First, you need to provide your test credentials. Playwright reads `.env` out of the box in the `frontend` directory. 
Create or update `frontend/.env` to include:
```env
E2E_TEST_EMAIL=your-test-account@example.com
E2E_TEST_PASSWORD=your-test-password
```

Then run (from the `frontend` directory):
```bash
# Headless run against Integration
npm run e2e:integration

# GUI run against Integration for debugging
npm run e2e:integration:ui
```
