public async Task<List<${entity_type}>> Get${entity_type}() {
    return this.DbContext.${entity_type}.ToListAsync();
}