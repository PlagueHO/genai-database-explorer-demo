namespace GenAIDBExplorer.Core.Models.SemanticModel;

/// <summary>
/// Defines methods for visiting semantic model entities.
/// </summary>
public interface ISemanticModelVisitor
{
    void VisitSemanticModel(ISemanticModel semanticModel);
    void VisitTable(SemanticModelTable table);
    void VisitView(SemanticModelView view);
    void VisitStoredProcedure(SemanticModelStoredProcedure storedProcedure);
    void VisitColumn(SemanticModelColumn column);
    void VisitIndex(SemanticModelIndex index);
}
