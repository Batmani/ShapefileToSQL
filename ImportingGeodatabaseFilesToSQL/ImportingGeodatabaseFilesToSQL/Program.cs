using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using NetTopologySuite.IO.ShapeFile.Extended;
using NetTopologySuite.IO.ShapeFile.Extended.Entities;
using NetTopologySuite.IO.Streams;
using GeoAPI.Geometries;
using System.Data.SqlClient;
using System.Data;
using System.ComponentModel;
using NetTopologySuite.IO;

using NetTopologySuite.Geometries;
using NetTopologySuite.Features;

namespace ImportingGeoDatabaseToSQL
{
    class Program
    {
        static void Main(string[] args)
        {
            ImportShapeFileIntoDatabase("path\\to\\shapefile")
        }

        public static void ImportShapeFileIntoDatabase(string filePath)
        {
            ShapeDataReader reader = new ShapeDataReader(filePath);

            GeometryFactory factory = new GeometryFactory();
            ShapefileDataReader shapeFileData = new ShapefileDataReader("", factory);
            DbaseFieldDescriptor[] fieldsInformation = shapeFileData.DbaseHeader.Fields;
            Envelope mb = reader.ShapefileBounds;
            IEnumerable<IShapefileFeature> result = reader.ReadByMBRFilter(mb);
            IAttributesTable columnAttributes = result.First().Attributes;
            IShapefileFeature[] rowInformation = result.ToArray();

            //CREATE TABLE COLUMNS
            RunSqlQuery(CreateTableColumns(columnAttributes, fieldsInformation));

            //INSERTING TABLE VALUES
            InsertTableValues(result.Count(), rowInformation).ForEach(x => RunSqlQuery(x));
        }


        public static string CreateTableColumns(IAttributesTable columnAttributes, DbaseFieldDescriptor[] fieldsInformation)
        {
            string[] tableColumns = columnAttributes.GetNames();

            string columnInformation = "";
            for (int i = 0; i < tableColumns.Length; i++)
            {
                columnInformation += tableColumns[i] + " " + SetSqlDataType(fieldsInformation[i].Length, fieldsInformation[i].DecimalCount, fieldsInformation[i].DbaseType) + ", ";
            }
            columnInformation = columnInformation.TrimEnd(',', ' ');

            string createTableString = "CREATE TABLE TestTable ( OBJECTID_1 int IDENTITY (1,1) PRIMARY KEY, Shape geometry, " + columnInformation + "); ";

            return createTableString;
        }

        public static List<string> InsertTableValues(int numberOfRows, IShapefileFeature[] rowInformation)
        {
            List<string> rowSqlQueries = new List<string>();
            for (int i = 0; i < numberOfRows; i++)
            {
                object[] rowValues = rowInformation[i].Attributes.GetValues();
                IGeometry geometryValue = rowInformation[i].Geometry;
                string valueString = "";
                foreach (var item in rowValues)
                {
                    if (item is string e)
                    {
                        valueString += "'" + item + "', ";
                    }
                    else
                    {
                        valueString += item + ", ";
                    }

                }
                string insertRowDataQuery = "INSERT INTO TestTable VALUES ( geometry::STPolyFromText('" + geometryValue + "'," + geometryValue.SRID + " ), " + valueString.TrimEnd(',', ' ') + ");";
                rowSqlQueries.Add(insertRowDataQuery);
            }

            return rowSqlQueries;
        }

        public static string SetSqlDataType(int length, int decimalCount, char dbPrefix)
        {
            //CLR type to SQL data type
            if (dbPrefix.Equals('N'))
            {
                if (decimalCount == 0)
                {
                    if (length < 11)
                    {
                        return "int";
                    }
                    else
                    {
                        return "long";
                    }
                }
                else
                {
                    return "numeric(38, 8)";
                }
            }

            if (dbPrefix.Equals('I'))
            {
                return "int";
            }

            if (dbPrefix.Equals('C'))
            {
                return "nvarchar(" + length + ")";
            }

            if (dbPrefix.Equals('F'))
            {
                return "numeric(38, 8)";
            }

            if (dbPrefix.Equals('L'))
            {
                return "bit";
            }

            if (dbPrefix.Equals('D'))
            {
                return "Date";
            }

            if (dbPrefix.Equals('B'))
            {
                return "geometry object";
            }

            throw new NotSupportedException(string.Format("The specified column name does not have a corresponding database type."));
        }


        public static void RunSqlQuery(string query)
        {
            using (SqlConnection connection = new SqlConnection("SQLServerconnectionstring\\ToyourDatabase"))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                command.ExecuteNonQuery();
            }
        }
    }
}
