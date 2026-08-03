## Context
In the context of an automotive workshop, customers tell service advisors the issues with their vehicles. This is not always easily understandable or quickly actionable.

## Requirement
- Create a minimal .Net API to use an Azure OpenAI model to understand the user's fault statement as a service advisor would, and respond with a JSON object that includes a rephrasing of the customer complaint in a manner that will be easily understood by worksop staff and a list of possible solution objects, each containing a possible issue and a suggestid fix/action.
Errors or misunderstandings should be handled cleanly.

- Create a small TypeScript test UI which can accept a user input string, call the API, and display the results cleanly. 

## Constraints
### Do
- Process user input in a clean and predictable fashion
- LLM responses should be in a set and repeatable format
- Errors should be handled and communicatedd cleanly
- Create logging for support and traceability

### Do NOT
- Deviate from the required pattern
- Store user prompts