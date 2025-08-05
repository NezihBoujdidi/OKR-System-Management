# Tensai OKR Management System - AI Component Technical Documentation

This document provides a detailed explanation of how the AI component processes user prompts, executes functions, manages context, and generates responses. The system is built using Semantic Kernel with support for OpenAI and Cohere LLM providers.

## Table of Contents

1. [Process Flow Overview](#process-flow-overview)
2. [Component Architecture](#component-architecture)
3. [Detailed Process Flow](#detailed-process-flow)
   - [User Input Processing](#user-input-processing)
   - [Intent Analysis](#intent-analysis)
   - [Function Execution](#function-execution)
   - [Response Generation](#response-generation)
4. [Chat History and Context Management](#chat-history-and-context-management)
5. [Team Management Operations](#team-management-operations)
6. [Adding New Functions/Intents](#adding-new-functionsintents)

## Process Flow Overview

The system follows these high-level steps to process user prompts:

1. User sends a message via the Chat API endpoint
2. The system analyzes the message to identify intent(s)
3. For each identified intent, the system executes the appropriate function
4. Results from function executions are recorded in chat history
5. A natural-language response is generated based on function results
6. The response is sent back to the user

## Component Architecture

The system is built around these core components:

- **ChatController**: API entry point that handles user requests
- **IntentProcessor**: Analyzes prompts and orchestrates function execution
- **FunctionService**: Contains actual functions that interact with the domain (via MediatR)
- **KernelService**: Manages the LLM interactions and chat context
- **EnhancedChatHistory**: Stores conversation history with rich context
- **PromptTemplateService**: Manages templates for generating consistent responses
- **ResponseGenerator**: Creates natural language responses from function results

## Detailed Process Flow

### User Input Processing

1. **API Request Handling**
   - The `ChatController.Chat()` method receives the user message
   - The controller initializes the user context (organization ID, user ID, etc.)

2. **Setting Up for Processing**
   - The controller calls `IntentProcessor.AnalyzePromptAsync()` with the user's message
   - The user message is recorded in the enhanced chat history

```csharp
// In ChatController.Chat()
var intentRequests = await _intentProcessor.AnalyzePromptAsync(request.Message);
var userMessage = EnhancedChatMessage.FromUser(request.Message);
userMessage.Metadata["AuthorName"] = userName;
_kernelService.GetEnhancedHistory().AddMessage(userMessage);
```

### Intent Analysis

3. **System Message Generation**
   - `IntentSystemMessageGenerator` creates specialized prompts for intent detection
   - The system looks at recent conversation history to provide context for entity references

4. **Context Enhancement**
   - Recent entity references (like team IDs and names) are extracted from history
   - This context helps the AI recognize when users refer to entities by name

5. **Intent Detection with LLM**
   - The system sends the enhanced system message + user prompt to the LLM
   - The LLM analyzes and returns structured JSON with identified intents and parameters

6. **Multiple Intent Handling**
   - The response is parsed into a list of `IntentRequest` objects
   - Each intent contains parameters extracted from the user's message

```csharp
// In IntentProcessor.AnalyzePromptAsync()
string systemMessage = _intentSystemMessageGenerator.GenerateIntentDetectionSystemMessage();
// Build context from recent entity references
// ...
string analysisResponse = await _kernelService.ExecuteSinglePromptAsync(enhancedSystemMessage, prompt);
// Parse the response into IntentRequest objects
// ...
```

### Function Execution

7. **Intent Processing**
   - For each identified intent, `IntentProcessor.ProcessMultipleIntentsAsync()` is called
   - The processor skips "General" intents which don't require function execution

8. **Function Selection & Parameter Preparation**
   - For each specific intent (like "CreateTeam"), the corresponding function is identified
   - Parameters are extracted and provided to the function along with user context

9. **Function Execution Path**
   - The function is executed via `ExecuteFunctionByIntent()` which contains a switch statement
   - Each function case maps to a corresponding method in `FunctionService`

10. **Team Lookup by Name**
    - For update/delete operations where a team name is provided:
      - The system searches for teams matching the name using `SearchTeamsAsync()`
      - If exactly one match is found, it uses that team's ID
      - If multiple matches are found, it asks for clarification
      - If no match is found, it falls back to the most recent team ID from history

11. **Domain Operation**
    - `FunctionService` calls the appropriate method in `MediatRService`
    - `MediatRService` sends commands/queries to the domain layer via MediatR

```csharp
// In IntentProcessor.ExecuteFunctionByIntent()
case "UpdateTeam":
    var teamId = parameters.GetValueOrDefault("teamId", null);
    var teamName = parameters.GetValueOrDefault("name", null);
    // ... determine teamId from name or history if needed ...
    var updateResult = await _functionService.UpdateTeamAsync(
        teamId,
        parameters.GetValueOrDefault("name", null),
        parameters.GetValueOrDefault("description", null),
        parameters.GetValueOrDefault("organizationId", userContext?.OrganizationId),
        parameters.GetValueOrDefault("teamManagerId", null));
    // ... handle result ...
```

### Response Generation

12. **Response Template Selection**
    - After function execution, the system needs to generate a natural language response
    - The `PromptTemplateService` selects the appropriate template based on the operation

13. **Response Template Population**
    - The template is populated with values from the function result
    - For example, a "TeamCreated" template is filled with the team name and description

14. **Multi-intent Response Consolidation**
    - If multiple intents were executed successfully, `ResponseGenerator` is used
    - It consolidates the multiple operations into a single coherent response

15. **Response Return to User**
    - The final response is returned to the user via the controller
    - The full chat history is also returned for stateless clients

```csharp
// In FunctionService.CreateTeamAsync() - template selection example
Dictionary<string, string> templateValues = new()
{
    { "teamName", result.Name },
    { "organizationId", organizationId }
};

result.PromptTemplate = !string.IsNullOrEmpty(description)
    ? _promptTemplateService.GetPrompt("TeamCreatedWithDescription", templateValues.Concat(new[] 
        { new KeyValuePair<string, string>("description", description) }).ToDictionary(x => x.Key, x => x.Value))
    : _promptTemplateService.GetPrompt("TeamCreated", templateValues);
```

## Chat History and Context Management

### Enhanced Chat History

The system maintains a rich conversation history that includes:

- User messages
- AI responses
- Function executions
- Entity references (like teams, users, objectives)

Each history item includes metadata such as:

- Entity type and ID (e.g., "Team" + UUID)
- Entity names (for human-readable references)
- Operation performed (e.g., "Create", "Update")
- Timestamp

```csharp
// Recording function execution in chat history
_kernelService.RecordFunctionExecution(
    resultItem.Intent,
    resultItem.Data,
    resultItem.EntityType,
    resultItem.EntityId,
    resultItem.Operation
);
```

### Entity Reference Tracking

The system specifically tracks entity references to allow natural follow-up questions:

1. When a function is executed, the entity ID and name are stored in chat history
2. When a user references an entity by name, the system attempts to find its ID
3. The ID is then used for the actual function execution

For example:
- User: "Create a team called Marketing"
- System: *Creates team with UUID and stores both the ID and name "Marketing"*
- User: "Update the description of Marketing team to 'Handles all marketing efforts'"
- System: *Looks up the ID for "Marketing" team, then updates that specific team*

## Team Management Operations

The system currently supports these operations for teams:

- **Create Team**: Creates a new team with name and optional description
- **Update Team**: Updates an existing team's properties (by ID or name)
- **Delete Team**: Removes a team (by ID or name)
- **Search Teams**: Finds teams by name or other criteria
- **Get Team Details**: Retrieves full information about a specific team
- **List Teams by Manager**: Shows all teams managed by a specific user
- **List Teams by Organization**: Shows all teams in an organization

Each operation follows this pattern:
1. Intent detection identifies the operation and parameters
2. The corresponding function in `FunctionService` is called
3. The function calls the appropriate method in `MediatRService`
4. `MediatRService` translates to domain commands/queries via MediatR
5. The result is returned and a natural language response is generated

## Adding New Functions/Intents

To add new functionality:

1. Add a new intent in `IntentSystemMessageGenerator.GenerateIntentDetectionSystemMessage()`
2. Add a new case in `IntentProcessor.ExecuteFunctionByIntent()`
3. Implement the corresponding function in `FunctionService`
4. Add the necessary MediatR commands/queries in `MediatRService`
5. Create response templates in `PromptTemplateService`

This structure makes the system extensible while maintaining clean separation of concerns.
