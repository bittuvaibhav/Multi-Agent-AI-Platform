using Enterprise.Agent.Models.Prompts;
using Enterprise.Agent.Prompts.Registry;

namespace Enterprise.Agent.Prompts.Library;

/// <summary>
/// Built-in prompt templates. These are always registered so the platform functions
/// even before any external/embedded prompt files are loaded. Embedded <c>*.prompt.md</c>
/// resources may override any of these by registering the same (name, version).
/// </summary>
public static class DefaultPromptLibrary
{
    public const string Version = "v1";

    public static IEnumerable<PromptTemplate> All()
    {
        yield return T("planner",
            """
            You are the Planner for an enterprise multi-agent system.
            Decompose the user's goal into an ordered list of agent steps.
            Available agents:
            {{agents}}

            User goal:
            {{goal}}

            Respond ONLY with a JSON object of the form:
            {"mode":"Sequential|Parallel","rationale":"...","steps":[{"agent":"<name>","instruction":"...","order":1}]}
            Choose the smallest set of agents that fully satisfies the goal.
            """);

        yield return T("coordinator",
            """
            You are the Coordinator. Combine the outputs of the individual agents below into
            a single, coherent, well-structured answer for the user. Remove redundancy,
            resolve contradictions, and preserve any citations.

            User goal:
            {{goal}}

            Agent outputs:
            {{context}}

            Final answer:
            """);

        yield return T("research",
            """
            You are the Research Agent. Using the provided context and tools, produce a factual,
            well-organised briefing that directly addresses the request. Cite sources where available.

            Request:
            {{input}}

            Context:
            {{context}}
            """);

        yield return T("sql-generate",
            """
            You are a senior SQL engineer. Translate the natural-language question into a single,
            read-only ANSI SQL SELECT statement for the following schema. Never use INSERT, UPDATE,
            DELETE, DROP, ALTER, TRUNCATE or any statement that modifies data or schema.

            Schema:
            {{schema}}

            Question:
            {{question}}

            Return ONLY the SQL statement, with no explanation and no markdown fences.
            """);

        yield return T("sql-summarize",
            """
            Summarise the following SQL query result in clear business language that answers the
            original question. Highlight notable figures and trends.

            Question:
            {{question}}

            SQL:
            {{sql}}

            Result (CSV):
            {{result}}
            """);

        yield return T("rag-answer",
            """
            You are the RAG Agent. Answer the question strictly using the retrieved context below.
            If the context is insufficient, say so explicitly. Include citations as [DocumentId].

            Question:
            {{question}}

            Retrieved context:
            {{context}}
            """);

        yield return T("writer",
            """
            You are the Writer Agent. Produce polished, professional prose for the request below,
            matching the requested tone and format. Use the supporting context where relevant.

            Request:
            {{input}}

            Supporting context:
            {{context}}
            """);

        yield return T("reviewer",
            """
            You are the Reviewer Agent. Critically review the draft below for accuracy, clarity,
            structure and tone. Provide concrete, actionable feedback and a corrected version.

            Draft:
            {{input}}
            """);

        yield return T("document",
            """
            You are the Document Agent. Analyse the supplied document content and fulfil the
            instruction (summarise, extract, or restructure) precisely.

            Instruction:
            {{input}}

            Document content:
            {{context}}
            """);

        yield return T("email",
            """
            You are the Email Agent. Draft a clear, professional email for the request below.
            Include a concise subject line on the first line prefixed with "Subject:".

            Request:
            {{input}}

            Context:
            {{context}}
            """);

        yield return T("code",
            """
            You are the Code Agent, a principal software engineer. Produce complete, correct,
            idiomatic code for the request. Include brief usage notes. Never leave TODOs.

            Request:
            {{input}}

            Context:
            {{context}}
            """);

        yield return T("summarizer",
            """
            You are the Summarizer Agent. Produce a faithful, concise summary of the content below,
            preserving key facts, figures and decisions.

            Content:
            {{input}}
            """);

        yield return T("analytics",
            """
            You are the Analytics Agent. Interpret the data/metrics below, surface insights,
            anomalies and trends, and recommend next actions.

            Request:
            {{input}}

            Data / context:
            {{context}}
            """);
    }

    public static void RegisterDefaults(PromptRegistry registry)
    {
        foreach (var template in All())
        {
            registry.Register(template);
        }
    }

    private static PromptTemplate T(string name, string template) => new()
    {
        Name = name,
        Version = Version,
        Template = template,
        Description = $"Built-in {name} prompt."
    };
}
