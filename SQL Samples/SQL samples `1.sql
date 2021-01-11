CREATE FUNCTION [dbo].[fn_ConvertTimeToSeconds]
(
	@time NVARCHAR(200)
)
RETURNS INT
AS
BEGIN
	SET @time = ISNULL(@time, '000:00:00')
	DECLARE @len INT = LEN(@time)
	DECLARE @hmsep INT = CHARINDEX(':', @time)
	DECLARE @mssep INT = CHARINDEX(':', @time, @hmsep + 1)
	RETURN (SELECT DATEDIFF(SECOND, 0,
		DATEADD(SECOND, CAST(SUBSTRING(@time, @mssep + 1, @len - @mssep) AS INT),
		DATEADD(MINUTE, CAST(SUBSTRING(@time, @hmsep + 1, @mssep - @hmsep - 1) AS INT), 
		DATEADD(HOUR,   CAST(SUBSTRING(@time, 1, @hmsep - 1) AS INT), 0))))
	)
END
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO