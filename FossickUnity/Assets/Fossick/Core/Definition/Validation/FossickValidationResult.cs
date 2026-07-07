using System.Collections.Generic;

namespace Fossick.Core.Definition.Validation
{
    public enum FossickValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum FossickValidationCategory
    {
        MapDefinition,
        GenerationRules,
        Template
    }

    public sealed class FossickValidationIssue
    {
        public FossickValidationSeverity severity;
        public FossickValidationCategory category;
        public string message;
        public int fragmentId;
        public int x = -1;
        public int y = -1;

        public FossickValidationIssue(FossickValidationSeverity severity, string message, int fragmentId = 0, int x = -1, int y = -1, FossickValidationCategory category = FossickValidationCategory.Template)
        {
            this.severity = severity;
            this.category = category;
            this.message = message;
            this.fragmentId = fragmentId;
            this.x = x;
            this.y = y;
        }
    }

    public sealed class FossickValidationResult
    {
        public readonly List<FossickValidationIssue> issues = new List<FossickValidationIssue>();

        public bool HasErrors
        {
            get
            {
                for (var i = 0; i < issues.Count; i++)
                {
                    if (issues[i].severity == FossickValidationSeverity.Error)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Add(FossickValidationSeverity severity, string message, int fragmentId = 0, int x = -1, int y = -1, FossickValidationCategory category = FossickValidationCategory.Template)
        {
            issues.Add(new FossickValidationIssue(severity, message, fragmentId, x, y, category));
        }
    }
}
