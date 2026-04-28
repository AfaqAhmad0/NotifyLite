# Contributing to NotifyLite

First off, thank you for considering contributing to NotifyLite! It's people like you that make open source tools great.

## How Can I Contribute?

### Reporting Bugs
Bugs are tracked as GitHub issues. When creating an issue, please use the Bug Report template and provide as much detail as possible to help us reproduce the issue.

### Suggesting Enhancements
Enhancement suggestions are also tracked as GitHub issues. Use the Feature Request template. Be sure to explain *why* this enhancement would be useful to most users.

### Pull Requests
1. Fork the repo and create your branch from `main`.
2. If you've added code that should be tested, add tests.
3. If you've changed APIs, update the documentation.
4. Ensure the test suite passes.
5. Make sure your code lints and follows the existing formatting.
6. Issue that pull request!

## Local Development Setup
1. Clone the repository locally.
2. Ensure you have the **.NET 8 SDK** installed.
3. Open `NotifyLite.sln` in Visual Studio or Rider.
4. Set the startup project to `NotifyLite` and build.

## Code Style
Please adhere to standard C# coding conventions and maintain the existing style of the repository. Use `GlobalUsings.cs` when bringing in new libraries that conflict between WinForms/WPF/WinRT.
