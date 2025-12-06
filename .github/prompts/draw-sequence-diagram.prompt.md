You are an expert software architect. I need a PlantUML Sequence Diagram that visualizes the execution flow of the [INSERT FUNCTION NAME HERE] feature.

**REFERENCE STYLE & FORMATTING RULES**
You must strictly follow the styling and logic depth of the provided reference standard:

1.  **Strict Hierarchical Numbering:** Every message must be numbered based on depth (e.g., 1 -> 1.1 -> 1.1.1 -> 1.1.1.1).
2.  **Participant Order:** Actor -> Frontend Page -> Controller -> Service(s) -> Repository(s) -> Database.
3.  **Naming Convention:** Use Interface names for backend components where applicable (e.g., `:IItemService`).
4.  **Visual Style:** Use the skinparams provided below (Blue lifelines, white backgrounds).

**MANDATORY TEXT PATTERNS**
You must use these EXACT phrases for specific actions:

1.  **Database Interaction:** Any call from a Repository to the Database must be labeled: **"Execute query"**.
2.  **UI Feedback:** When the code returns a string or error to the frontend that is shown to the user, you must label the final step as: **"Display message 'ACTUAL STRING FROM CODE'"**.

**PARTICIPANTS**
Map the code to these specific participant types:

- **Actor:** The User.
- **Frontend:** The specific Page/Component (e.g., `BulkUploadPage`).
- **Controller:** The Backend API Controller.
- **Service:** The Domain Service layer (prefix with `:`).
- **Repositories:** All distinct repositories (prefix with `:`).
- **Database:** A single participant named "Database" at the far right.

**LOGIC & FLOW REQUIREMENTS**

1.  **Entry Point:** Start with User -> Page (Access/POST).
2.  **Service Logic:**
    - **Loops:** Use `loop` blocks for iterations (e.g., `[For each item]`).
    - **Validations:** Use `alt` / `else` blocks for logical branches (e.g., `[category == null]`).
3.  **Return Path:**
    - Use dashed arrows (`-->`) for returns.
    - Ensure the final return to the user includes the "Display message" text as defined above.

**PLANTUML CODE**
Generate ONLY the PlantUML code inside `@startuml` and `@enduml`.

```plantuml
@startuml
skinparam sequence {
    ParticipantBackgroundColor #87CEFA
    ParticipantBorderColor Black
    ActorBackgroundColor White
    ActorBorderColor Black
    ArrowColor Black
    LifeLineBorderColor Black
    LifeLineBackgroundColor #87CEFA

    ParticipantFontName Arial
    ParticipantFontSize 13
    ParticipantFontColor Black
    ActorFontName Arial
    ActorFontSize 13
}

' Define Participants
actor "User" as User
participant "[FrontEndPage]" as Page
participant "[Controller]" as Controller
participant ":[IService]" as Service
participant ":[IRepository]" as Repo
participant "Database" as DB

' START SEQUENCE EXAMPLE
' User -> Page: 1: Access Page
' Page -> Controller: 1.1: POST Action
' activate Controller
' Controller -> Service: 1.1.1: Process()
' activate Service

'    Service -> Repo: 1.1.1.1: GetById()
'    activate Repo
'    Repo -> DB: 1.1.1.1.1: Execute query
'    activate DB
'    DB --> Repo: 1.1.1.1.2: Return result
'    deactivate DB
'    Repo --> Service: 1.1.1.2: Return entity
'    deactivate Repo

'    alt [entity == null]
'        Service --> Controller: 1.1.1.3: Return error
'        Controller --> Page: 1.1.1.3.1: Return response
'        Page -> User: 1.1.1.3.1.1: Display message "Entity not found"
'    end

' deactivate Service
' deactivate Controller

@enduml
```
