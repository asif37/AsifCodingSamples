CREATE FUNCTION [dbo].[fn_timedivide]
(
	@UpperTime TIME,
	@DownTime TIME
)
RETURNS DECIMAL(18,2)
AS
BEGIN
	declare @uppperValue decimal(18,2) = 0
	declare @downValue decimal(18,2) = 0
	declare @result decimal(18,2) = 0


	SET @uppperValue = datediff(MINUTE, 0, @UpperTime)
	SET @downValue = datediff(MINUTE, 0, @DownTime)
	
	SET @result = (@uppperValue / @downValue)
	
	RETURN @result

END
GO