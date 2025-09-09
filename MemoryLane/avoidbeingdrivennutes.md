Alright, to avoid being driven nuts here - let's please stop asking me to double check things. We just double checked this:

   public FreshFruitNeed() : base()
    {
    }

All of the Need classes are like this, DynamicNeed has:

#pragma warning disable CS8618, CS9264
        protected DynamicNeed() : base()
#pragma warning restore CS8618, CS9264
        {
        }

without the explicit null. Previously, it had an explicit null as an argument but the behavior was exactly the same, same exception.

Here are some of the methods in our pipeline from Core:

public void AddOrRemoveNeedsAsAppropriate()
		{
			List<NeedDef> allDefsListForReading = DefDatabase<NeedDef>.AllDefsListForReading;
			for (int i = 0; i < allDefsListForReading.Count; i++)
			{
				try
				{
					NeedDef needDef = allDefsListForReading[i];
					if (ShouldHaveNeed(needDef))
					{
						if (TryGetNeed(needDef) == null)
						{
							AddNeed(needDef);
						}
					}
					else if (TryGetNeed(needDef) != null)
					{
						RemoveNeed(needDef);
					}
				}
				catch (Exception ex)
				{
					Log.Error("Error while determining if " + pawn.ToStringSafe() + " should have Need " + allDefsListForReading[i].ToStringSafe() + ": " + ex);
				}
			}
		}


And here is our ShouldHaveNeed:

