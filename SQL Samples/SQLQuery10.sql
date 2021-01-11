SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE FUNCTION [dbo].[fn_timesumNumber]
(
	@val INT
)
RETURNS nvarchar(50)
AS
BEGIN
	-- Declare the return variable here
	DECLARE @x nvarchar(50)

	SET @x = FORMAT(CAST((@val / 60) as int),'0#') + ':' + FORMAT(CAST((@val % 60) as int), '0#') + ':00'
	RETURN @x

END
GO