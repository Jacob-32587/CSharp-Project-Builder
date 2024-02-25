public class ${DaoClassName}
{
    private readonly Context DbContext;

    public ${DaoClassName}(Context DbContext)
    {
        this.DbContext = DbContext;
    }
    ${operation_method_code}
}