# JobsParser.AutoApplyService

A service that automatically applies for job offers using a JSON DSL-based workflow engine with browser automation.

## Overview

The Auto Apply Service is designed to automate the job application process by:

1. Retrieving job offers from the database that need to be applied to
2. Determining the appropriate workflow based on the job source/URL
3. Executing the workflow using browser automation with Playwright
4. Marking the job as applied when successful

## JSON DSL (Domain-Specific Language)

The service uses a JSON-based DSL to define workflows for different job sites. These workflows are composed of commands that represent browser actions like clicking, filling forms, uploading files, etc.

### Command Types

- `sequence`: Executes a series of commands in order
- `ifelse`: Conditional execution based on a condition
- `foreach`: Loops through a collection of items
- `click`: Clicks on an element
- `fill`: Fills a form field with a value
- `upload`: Uploads a file to a file input
- `waitfor`: Waits for an element to appear on the page
- `navigate`: Navigates to a URL
- `exists`: Checks if an element exists on the page
- `genericformfill`: Automatically fills form fields using a configuration file

### Example Workflow

```json
{
  "type": "sequence",
  "commands": [
    {
      "type": "navigate",
      "url": "${JobUrl}",
      "waitUntil": "networkidle"
    },
    {
      "type": "click",
      "selector": ".apply-button",
      "waitForNavigation": true
    },
    {
      "type": "genericformfill",
      "configFilePath": "Configurations/potential_form_data.json"
    },
    {
      "type": "click",
      "selector": "button[type='submit']",
      "waitForNavigation": true
    }
  ]
}
```

## Generic Form Fill

The generic form fill command allows you to automatically fill web forms without knowing the exact structure of the form in advance. It uses a configuration file that defines:

1. Fields to look for (name, email, etc.)
2. Multiple possible selectors for each field
3. The value to fill in
4. The type of form element (text, select, checkbox, radio, file)

The command will try each selector for a field and use the first one that matches. Fields not found on the page are ignored.

### Example Form Field Configuration

```json
[
  {
    "fieldName": "FirstName",
    "possibleSelectors": [
      "#firstName",
      "input[name='firstName']",
      "input[name='first_name']"
    ],
    "dataValue": "${FirstName}",
    "fieldType": "text"
  },
  {
    "fieldName": "AgreeToTerms",
    "possibleSelectors": [
      "#termsAndConditions",
      "input[name='agree']"
    ],
    "dataValue": "true",
    "fieldType": "checkbox"
  }
]
```

## Configuration

The service is configured through the `appsettings.json` file:

```json
"AutoApplyService": {
  "PollingIntervalSeconds": 60,
  "WorkflowsDirectory": "Workflows",
  "MaxConcurrentInstances": 2,
  "Headless": true,
  "DefaultResumePath": "C:\\Path\\To\\Your\\Resume.pdf",
  "DefaultCoverLetterPath": "C:\\Path\\To\\Your\\CoverLetter.pdf",
  "ActionDelayMs": 500,
  "DefaultTimeoutMs": 30000,
  "AutoProcessNewLinks": true
}
```

### Configuration Options

- `PollingIntervalSeconds`: How often to check for new jobs to apply to
- `WorkflowsDirectory`: Directory where workflow JSON files are stored
- `MaxConcurrentInstances`: Maximum number of concurrent browser instances
- `Headless`: Whether to run browsers in headless mode
- `DefaultResumePath`: Path to the default resume file
- `DefaultCoverLetterPath`: Path to the default cover letter file
- `ActionDelayMs`: Delay between automation actions in milliseconds
- `DefaultTimeoutMs`: Default timeout for actions in milliseconds
- `AutoProcessNewLinks`: Whether to automatically process new job links

## Creating Custom Workflows

To create a custom workflow for a specific job site:

1. Create a new JSON file in the Workflows directory with a name that matches the site (e.g., `linkedin.json`, `indeed.json`)
2. Define the workflow using the available command types
3. Use variable placeholders like `${JobUrl}` or `${ResumePath}` to reference context variables

## Architecture

The service uses several design patterns:

- **Command Pattern**: Each action is a self-contained command object
- **Composite Pattern**: Allows commands to be nested within other commands
- **Interpreter Pattern**: Parses and executes the JSON DSL
- **State Machine**: Manages transitions between steps in the application process 