namespace Enterprise.Agent.Models.Enums;

/// <summary>Well-known agent roles available in the platform.</summary>
public enum AgentRole
{
    Coordinator = 0,
    Planner = 1,
    Research = 2,
    Sql = 3,
    Rag = 4,
    Writer = 5,
    Reviewer = 6,
    Document = 7,
    Email = 8,
    Code = 9,
    Summarizer = 10,
    Analytics = 11
}

/// <summary>Discrete capabilities an agent may advertise for planning/selection.</summary>
[Flags]
public enum AgentCapability
{
    None = 0,
    TextGeneration = 1 << 0,
    Retrieval = 1 << 1,
    DatabaseQuery = 1 << 2,
    WebSearch = 1 << 3,
    Summarization = 1 << 4,
    CodeGeneration = 1 << 5,
    Review = 1 << 6,
    DocumentProcessing = 1 << 7,
    Email = 1 << 8,
    Analytics = 1 << 9,
    Planning = 1 << 10,
    Orchestration = 1 << 11
}

/// <summary>Execution topology used by the orchestrator for a plan.</summary>
public enum WorkflowMode
{
    Sequential = 0,
    Parallel = 1
}

/// <summary>Lifecycle state of an execution step or an overall run.</summary>
public enum ExecutionStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    TimedOut = 4,
    Cancelled = 5,
    Skipped = 6
}

/// <summary>Role of a chat message in a conversation.</summary>
public enum MessageRole
{
    System = 0,
    User = 1,
    Assistant = 2,
    Tool = 3
}

/// <summary>Scope/lifetime of a memory record.</summary>
public enum MemoryScope
{
    Conversation = 0,
    LongTerm = 1,
    Semantic = 2
}

/// <summary>Supported vector store backends.</summary>
public enum VectorProvider
{
    Postgres = 0,
    AzureAiSearch = 1
}

/// <summary>Supported document formats for RAG ingestion.</summary>
public enum DocumentType
{
    Text = 0,
    Markdown = 1,
    Pdf = 2,
    Word = 3
}

/// <summary>Classification of a SQL statement for the safety guard.</summary>
public enum SqlStatementKind
{
    Unknown = 0,
    Select = 1,
    Insert = 2,
    Update = 3,
    Delete = 4,
    Ddl = 5,
    Administrative = 6
}
