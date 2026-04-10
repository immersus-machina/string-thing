using System.Data;

namespace StringThing.SqlClient;

internal record SqlServerTable(DataTable Table, string TypeName);
