namespace Models.Objects;
using Microsoft.CodeAnalysis;

public class ProjectSelection(Project project) {
    public Project Project = project;

    public override string ToString() {
        return Project.Name;
    }
}
