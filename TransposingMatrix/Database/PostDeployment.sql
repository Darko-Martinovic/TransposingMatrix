-------------------------------------------------------------------------
-- Make the stored procedure parameters default
-------------------------------------------------------------------------
ALTER PROCEDURE [dbo].[Transposing]
	@Query [nvarchar](max),
	@Params [nvarchar](4000)=NULL,
	@Rco [smallint]=0,
	@KeyValueOption [smallint]=0,
     @ColumnMapping [nvarchar](4000) = NULL,
	@TableName [nvarchar](256) = NULL
AS EXTERNAL NAME [SimpleTalk.SQLCLR.Matrix].[StoredProcedures].[Transposing];
GO
-------------------------------------------------------------------------
--Transfer the stored procedure
-------------------------------------------------------------------------
IF EXISTS ( SELECT * 
            FROM   sysobjects 
            WHERE  id = object_id(N'[MATRIX].[Transposing]') 
                   and OBJECTPROPERTY(id, N'IsProcedure') = 1 )
BEGIN
    DROP PROCEDURE [MATRIX].[Transposing];
END

ALTER SCHEMA [MATRIX] TRANSFER [dbo].[Transposing];

-------------------------------------------------------------------------
--Transfer the function
-------------------------------------------------------------------------
IF EXISTS ( SELECT * 
			FROM   sysobjects 
			WHERE  id = object_id(N'[MATRIX].[Help]') 
				   and type = N'FT' )
BEGIN
	DROP FUNCTION [MATRIX].[Help];
END

ALTER SCHEMA  [MATRIX] TRANSFER dbo.Help;