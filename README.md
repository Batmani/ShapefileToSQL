# ShapefileToSQL
Repository containing conversion of a Shapefile to a spatial SQL database

This should be treated as a save backup dump, code has not been formatted or optimized, it is merely a backup and intended to show how to convert shapefiles into SQL spatial tables by parsing data values into an SQL query string. Uses NetTopologySuite.

NOTE: If you are intending to use the SQL Insert query to individually run the query for more than 100 rows - Don't. It is NOT efficient and will result in memory issues - instead, please parse the data values into a .CSV (or similar) and simply import that table of information into the database.

Warning: Beta version that is not intended for development use, no test cases implemented etc.
