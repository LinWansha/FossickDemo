using System.Collections.Generic;

namespace Fossick.Core.Validation
{
    public enum FossickValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public sealed class FossickValidationIssue
    {
        public FossickValidationSeverity severity;
        public string message;
        public int fragmentId;
        public int x = -1;
        public int y = -1;

        public FossickValidationIssue(FossickValidationSeverity severity, string message, int fragmentId = 0, int x = -1, int y = -1)
        {
            this.severity = severity;
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

        public void Add(FossickValidationSeverity severity, string message, int fragmentId = 0, int x = -1, int y = -1)
        {
            issues.Add(new FossickValidationIssue(severity, message, fragmentId, x, y));
        }
    }
}
