#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies.MaxBots
{
	public class RobStratSync : Strategy
	{
		
		#region Vars
		private EMA LongEma;
		private EMA ShortEma;
		
		// Long BE
		private double LongTs;
		private double LongCloseValue = 0.0;
		private bool LongTrackPosition = false;
		
		//Short BE
		private double ShortTs;
		private double ShortCloseValue = 0.0;
		private bool ShortTrackPosition = false;
		#endregion
		
		#region OnStateChange
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Specific type of pattern detection. The Rob pattern.";
				Name										= "RobStratSync";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				LongEmaPeriod = 8;
				LongRiskReward = risk_to_reward.OneToTwo;
				LongStopLoss = 50;
				
				ShortEmaPeriod = 8;
				ShortRiskReward = risk_to_reward.OneToTwo;
				ShortStopLoss = 50;
				
				LongTrailingStopValue					= 200;
				LongMoveTrailStop					= 150;
				
				ShortTrailingStopValue					= 200;
				ShortMoveTrailStop					= 150;
				
				Contracts = 1;
				
				UseLongs = false;
				UseShorts = false;

			}
			else if (State == State.Configure)
			{
				LongEma = EMA(LongEmaPeriod);
				ShortEma = EMA(ShortEmaPeriod);
				AddChartIndicator(LongEma);
				AddChartIndicator(ShortEma);
				
			}
			else if (State == State.Terminated) 
			{
				LongEma = null;
				ShortEma = null;
			}
		}
		#endregion

		#region TradeLogic
		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;
			
			TradeLogic();
		}
		
		private void TradeLogic() {
			OrderHandling();
			
			Conditions c = SearchForEntryConditions();
			
			if ( c.LongCondition )
			{
				EnterLong(Contracts, "RobStratSync Long");
				SetStopLoss(CalculationMode.Ticks, LongStopLoss);
				SetProfitTarget(CalculationMode.Ticks, LongStopLoss * GetRiskRatio(LongRiskReward));
				Draw.ArrowUp(this, "Up Arrow" + CurrentBar, true, 0, Low[0] - TickSize * 2, Brushes.Green);
			}
			
			if ( c.ShortCondition ) 
			{
				EnterShort(Contracts, "RobStratSync Short");
				SetStopLoss(CalculationMode.Ticks, ShortStopLoss);
				SetProfitTarget(CalculationMode.Ticks, ShortStopLoss * GetRiskRatio(ShortRiskReward));
				Draw.ArrowDown(this, "Down Arrow" + CurrentBar, true, 0, High[0] + TickSize * 2, Brushes.Red);
			}
		}
		
		private Conditions SearchForEntryConditions()
		{
			bool longCondition = BarsInProgress == 0 && Close[0] > LongEma[0] && Close[1] < Open[1] && Low[0] < Low[1] && Close[0] < Open[1] && Close[0] > Close[1] && Position.MarketPosition == MarketPosition.Flat && UseLongs;
			bool shortCodition = BarsInProgress == 0 && Close[0] < ShortEma[0] && Close[1] > Open[1] && High[0] > High[1] && Close[0] > Open[1] && Close[0] < Close[1] && Position.MarketPosition == MarketPosition.Flat && UseShorts;
			
			return new Conditions{
				LongCondition = longCondition,
				ShortCondition = shortCodition
			};
		}
		
		private void OrderHandling() {
			
			if ( Position.MarketPosition == MarketPosition.Long )
			{
				// Move order to break even after # of ticks occurred
				if ( Close[0] > Position.AveragePrice + LongMoveTrailStop * TickSize && LongTrackPosition == false ) {
					SetStopLoss(CalculationMode.Price, Position.AveragePrice - 0 * TickSize);
					LongCloseValue = Close[0];
					LongTrackPosition = true;
					LongTs = LongTrailingStopValue;
				}
			}
			
			if ( Position.MarketPosition == MarketPosition.Short )
			{
				// Move order to break even after # of ticks occurred
				if ( Close[0] < Position.AveragePrice - ShortMoveTrailStop * TickSize && ShortTrackPosition == false ) {
					SetStopLoss(CalculationMode.Price, Position.AveragePrice + 0 * TickSize);
					ShortCloseValue = Close[0];
					ShortTrackPosition = true;
					ShortTs = ShortTrailingStopValue;
				}
			}
			
			if ( Position.MarketPosition == MarketPosition.Flat )
			{
				
			}
		}
		#endregion
		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Long Ema Length", Order=1, GroupName="Long Parameters")]
		public int LongEmaPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Long Risk/Reward Ratio", Order = 8, GroupName = "Long Parameters")]
   		public risk_to_reward LongRiskReward { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Long Stop Loss", Order=20, GroupName="Long Parameters")]
		public double LongStopLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Short Ema Length", Order=1, GroupName="Short Parameters")]
		public int ShortEmaPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name = "Short Risk/Reward Ratio", Order = 8, GroupName = "Short Parameters")]
   		public risk_to_reward ShortRiskReward { get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Short Stop Loss", Order=40, GroupName="Short Parameters")]
		public double ShortStopLoss
		{ get; set; }
		
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Number of Contracts", Order=40, GroupName="Parameters")]
		public int Contracts
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Trailing Stop Value", Order=30, GroupName="Long Parameters")]
		public int LongTrailingStopValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Move Trail Stop", Order=40, GroupName="Long Parameters")]
		public int LongMoveTrailStop
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Trailing Stop Value", Order=30, GroupName="Short Parameters")]
		public int ShortTrailingStopValue
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Move Trail Stop", Order=40, GroupName="Short Parameters")]
		public int ShortMoveTrailStop
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Use Shorts", Order=2, GroupName="General Settings")]
		public bool UseShorts { get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Longs", Order=3, GroupName="General Settings")]
		public bool UseLongs { get; set; }
		
		#endregion
		
		#region Classes
		private class Conditions
		{
			public bool LongCondition { get; set; }
			public bool ShortCondition { get; set; }
		}
		
		public enum risk_to_reward
		{
			[Description( "1/0.5" )]
			OneToHalf,
			[Description( "1/1" )]
			OneToOne,
			[Description( "1/2" )]
			OneToTwo,
			[Description( "1/3" )]
			OneToThree,
			[Description( "1/4" )]
			OneToFour,
			[Description( "1/5" )]
			OneToFive,
			[Description( "1/6" )]
			OneToSix,
			[Description( "1/7" )]
			OneToSeven,
			[Description( "1/8" )]
			OneToEight,
			[Description( "1/9" )]
			OneToNine,
			[Description( "1/10" )]
			OneToTen
		}
		#endregion
		
		#region Helpful Functions
		private double GetRiskRatio( risk_to_reward revards)
		{	
			double ratio = 1;

			switch ( revards )
			{
				case risk_to_reward.OneToHalf:
					ratio = 0.5;
					break;
				case risk_to_reward.OneToOne:
					ratio = 1;
					break;
				case risk_to_reward.OneToTwo:
					ratio = 2;
					break;
				case risk_to_reward.OneToThree:
					ratio = 3;
					break;
				case risk_to_reward.OneToFour:
					ratio = 4;
					break;
				case risk_to_reward.OneToFive:
					ratio = 5;
					break;
				case risk_to_reward.OneToSix:
					ratio = 6;
					break;
				case risk_to_reward.OneToSeven:
					ratio = 7;
					break;
				case risk_to_reward.OneToEight:
					ratio = 8;
					break;
				case risk_to_reward.OneToNine:
					ratio = 9;
					break;
				case risk_to_reward.OneToTen:
					ratio = 10;
					break;
			}

			return ratio;
		}
		#endregion
		
	}
}
