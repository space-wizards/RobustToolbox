using System.Collections.Generic;

namespace Robust.Client.UserInterface
{
    public class FileDialogFilters
    {
        public FileDialogFilters(IReadOnlyList<Group> groups)
        {
            Groups = groups;
        }

        public FileDialogFilters(params Group[] groups) : this((IReadOnlyList<Group>) groups)
        {
        }

        public IReadOnlyList<Group> Groups { get; }

        public class Group
        {
            public Group(IReadOnlyList<string> extensions)
            {
                Extensions = extensions;
            }

            public Group(params string[] extensions) : this((IReadOnlyList<string>) extensions)
            {
            }

            public IReadOnlyList<string> Extensions { get; }
        }
    }
}
