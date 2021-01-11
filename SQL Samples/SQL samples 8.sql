CREATE FUNCTION [dbo].[fn_timediff]
(
	@Start TIME,
	@End TIME
)
RETURNS TIME
AS
BEGIN
	-- Declare the return variable here
	DECLARE @Diff TIME
	DECLARE @x INT

	SET @x = datediff (s, @Start, @End)

	SELECT @Diff = convert(time, dateadd(s, @x, convert(datetime2, '0001-01-01')), 108)

	-- Return the result of the function
	RETURN @Diff

END
GO