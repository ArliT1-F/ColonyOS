ColonyOS — Colony Intelligence & Management

Complete Development Specification

Tagline: Your colony. Your data. Your decisions.

Project type: RimWorld mod
Primary target: RimWorld 1.6
Secondary target: RimWorld 1.7
Gameplay dependency: RimWorld Core only; no DLC required
UI dependencies: External UI frameworks are permitted and encouraged when necessary to achieve the intended visual quality.

⸻

1. Project Vision

Build ColonyOS, a comprehensive colony intelligence and management system for RimWorld.

ColonyOS should function like an advanced operating system for managing a colony. It continuously collects, organizes, analyzes and presents information about the player’s colony, its economy, infrastructure, population, production, logistics and surrounding regional market.

The mod should help players understand:

* What is happening inside their colony?
* Why is it happening?
* What is likely to happen next?
* Where are the current inefficiencies?
* What resources or infrastructure are becoming bottlenecks?
* What should the colony prioritize?
* What commercial opportunities exist in the surrounding region?

The mod should not automatically take control away from the player. It should provide actionable intelligence, forecasts and recommendations while leaving decisions to the player.

ColonyOS is an information and decision-support system, not an autonomous colony manager.

2. Critical Visual Direction — Modern Web-Based Admin Dashboard

This is one of the most important requirements of the entire project.

ColonyOS must look like a modern web-based SaaS administration dashboard embedded inside RimWorld.

Think of professional analytics platforms, enterprise monitoring dashboards, financial management software and infrastructure control panels.

It must NOT look like a conventional RimWorld mod interface.

Do not create a boring interface consisting primarily of:

* Vanilla RimWorld windows.
* Vertically stacked text sections.
* Basic buttons and labels.
* Repetitive information boxes.
* Long lists of numerical values.
* Tables used as the primary way to present every type of information.

The interface should be visually rich, information-dense, interactive and professionally designed.

2.1 Dashboard layout

Create a modern desktop-application-style interface with:

* A persistent navigation sidebar.
* A central dashboard workspace.
* Contextual information panels.
* Responsive multi-column layouts.
* Variable-sized cards and widgets.
* Interactive charts and visualizations.
* Search, filtering and sorting.
* Expandable analytical panels.
* Drill-down navigation.
* Configurable themes and visual preferences.

The interface should feel like a complete management application rather than a collection of mod windows.

2.2 Main Overview dashboard

The Overview page must function as a genuine colony command centre.

It should present important information immediately, without requiring the player to navigate through several departments.

Include dashboard elements such as:

KPI cards

* Total colony wealth.
* Current power generation.
* Current power consumption.
* Battery charge.
* Food reserves.
* Population.
* Production output.
* Important resource quantities.

Each KPI should display relevant contextual information, such as:

* Current value.
* Historical change.
* Trend direction.
* Status indicator.
* Warning or threshold information, where applicable.

Analytical visualizations

* Power generation versus consumption.
* Colony wealth over time.
* Resource distribution.
* Production output trends.
* Workforce allocation.
* Food reserve projections.
* Regional merchant demand rankings.

Operational intelligence

* Active warnings.
* Predicted shortages.
* Production bottlenecks.
* Idle infrastructure.
* Labour inefficiencies.
* Upcoming trade opportunities.
* Important recent changes.

The dashboard should allow players to click a metric or visualization and navigate directly to its corresponding department.

2.3 Rich data visualization

Use appropriate visualizations instead of displaying every metric as text.

Examples include:

* Line charts for historical trends.
* Bar charts for production and consumption comparisons.
* Area charts for power generation and consumption.
* Donut charts for resource and wealth distribution.
* Stacked charts for workforce allocation.
* Heatmaps for production bottlenecks and storage utilization.
* Progress indicators for storage capacity and resource reserves.
* Ranking visualizations for merchant demand.
* Forecast charts showing projected values and uncertainty ranges.

Charts should support tooltips, selectable time ranges, comparisons and filtering wherever practical.

Do not fabricate data simply to make a chart look populated. If historical data is unavailable, communicate that clearly.

2.4 Non-linear interface design

Avoid using the same vertical layout for every department.

Do not make every screen follow:

Title → Description → Information → Table → Next section.

Instead, use:

* Multiple cards arranged in responsive grids.
* Large charts beside smaller information panels.
* Side-by-side comparisons.
* Expandable analytical panels.
* Contextual detail panels.
* Tabs for different data perspectives.
* Drill-down views.
* Variable-width and variable-height components.

The interface should be dynamic and information-dense without becoming cluttered.

2.5 Visual identity

Use a professional dark industrial aesthetic by default.

The design should include:

* Carefully selected accent colours.
* Clear typography and visual hierarchy.
* Subtle gradients and depth.
* Refined panel borders.
* Rounded corners.
* Consistent spacing.
* Professional iconography.
* Colour-coded statuses.
* Smooth transitions.
* Interactive hover states.
* Subtle animations.

Provide alternative themes where practical, such as:

* Dark Industrial.
* Midnight.
* Neutral Professional.

Allow customization of accent colours, panel backgrounds, chart colours, typography and animation intensity where supported.

2.6 UI framework evaluation

External UI framework dependencies are explicitly permitted.

Do not limit the project to vanilla RimWorld IMGUI if it cannot deliver the required visual quality.

Investigate and evaluate frameworks such as:

* RimUI Framework: Evaluate its components, layout system, charts, styling, animations and compatibility.
* Cosmere.Lightweave: Evaluate its layout, component, typography, theming and navigation capabilities.
* Other maintained RimWorld UI frameworks if they offer a better technical foundation.

Do not blindly select a framework based on its feature list.

Before choosing one, verify:

* Actual API availability.
* Documentation quality.
* Source code and licensing.
* Maintenance activity.
* RimWorld 1.6 compatibility.
* RimWorld 1.7 compatibility.
* Performance characteristics.
* Component availability.
* Compatibility with other common RimWorld UI mods.
* Distribution and dependency requirements.

Select the smallest practical dependency set that can deliver the intended interface.

Declare required dependencies correctly in the mod metadata and document installation requirements.

Do not bundle third-party framework code unless its license explicitly permits it.

Important architectural rule: The analytics engine, forecasting systems and gameplay data collection must remain independent of the UI framework. The interface should consume stable internal data models so the UI framework can be replaced or upgraded without rewriting the underlying intelligence systems.

3. Intelligence Departments

ColonyOS must contain seven interconnected intelligence departments.

Each department must have its own specialized interface, relevant metrics, visualizations and analytical functionality.

3.1 Power Intelligence

Monitor and analyze the colony’s electrical infrastructure.

Features:

* Total power generation.
* Total power consumption.
* Net power balance.
* Battery charge and discharge.
* Battery capacity.
* Power surplus and deficit.
* Historical power trends.
* Generator performance.
* Power network status.
* Building-level consumption where the game exposes reliable data.
* Identification of excessive power consumption.
* Identification of underperforming or inactive generators.
* Forecasts of potential power shortages.

Visualizations:

* Generation versus consumption chart.
* Battery charge history.
* Power balance indicators.
* Generator contribution breakdown.
* Historical power trends.

Predictive functionality:

* Estimate when batteries may become depleted.
* Identify recurring power shortages.
* Detect increasing consumption.
* Warn about insufficient generation capacity.

Do not claim building-level precision if the game API does not expose reliable measurements. Clearly distinguish measured values from estimates.

3.2 Production Intelligence

Monitor the colony’s production infrastructure and manufacturing efficiency.

Features:

* Active production bills.
* Production rates.
* Workbench utilization.
* Idle workbenches.
* Production bottlenecks.
* Ingredient shortages.
* Required materials.
* Available ingredients.
* Production queues.
* Workbench availability.
* Production history.
* Estimated completion times where sufficient information exists.
* Identification of inefficient production setups.

Visualizations:

* Production output charts.
* Workbench utilization.
* Production comparison charts.
* Ingredient availability indicators.
* Bottleneck rankings.
* Production history.

Predictive functionality:

* Identify upcoming ingredient shortages.
* Estimate whether current production can meet demand.
* Highlight underutilized production infrastructure.
* Identify production chains affected by missing materials.

3.3 Logistics Intelligence

Analyze resource storage, distribution and movement throughout the colony.

Features:

* Total storage capacity.
* Current storage utilization.
* Stockpile distribution.
* Resource quantities.
* Resource locations.
* Storage availability.
* Overfilled stockpiles.
* Inaccessible or poorly positioned resources where detectable.
* Hauling-related bottlenecks.
* Resources scattered across multiple storage locations.
* Frequently requested but inconveniently stored materials.

Visualizations:

* Storage utilization charts.
* Resource distribution.
* Stockpile capacity indicators.
* Resource availability rankings.
* Storage bottleneck indicators.

Predictive functionality:

* Predict storage overflow.
* Identify resources likely to run out.
* Identify storage capacity problems.
* Highlight inefficient storage distribution.

Do not invent hauling statistics or pathfinding information that cannot be reliably collected.

3.4 Population Intelligence

Analyze the colony’s workforce, skills and labour allocation.

Features:

* Population overview.
* Skill distribution.
* Work priorities.
* Available workforce.
* Assigned work.
* Idle pawns.
* Workload distribution.
* Skill shortages.
* Labour bottlenecks.
* Work restrictions.
* Production-related workforce availability.
* Work assignment conflicts where detectable.

Visualizations:

* Skill distribution charts.
* Workforce allocation.
* Workload comparisons.
* Labour availability indicators.
* Skill and work-type breakdowns.

Predictive functionality:

* Identify workforce shortages.
* Detect overburdened work areas.
* Highlight underutilized skilled workers.
* Estimate how workforce availability affects production.

Do not interfere with existing work priorities or automatically change pawn assignments.

3.5 Economic Intelligence

Provide a financial overview of the colony.

Features:

* Total colony wealth.
* Wealth composition.
* Inventory market value.
* Resource value distribution.
* Historical wealth trends.
* Trade transactions.
* Purchase history.
* Sale history.
* Profitability analysis.
* Resource value comparisons.
* Economic growth and decline.
* Significant changes in colony wealth.

Visualizations:

* Colony wealth history.
* Wealth composition charts.
* Purchase versus sale comparisons.
* Transaction history.
* Resource value rankings.
* Economic trend indicators.

Predictive functionality:

* Identify potential economic problems.
* Highlight valuable resources.
* Identify resources that may be worth selling.
* Highlight unusual economic changes.

Clearly distinguish market value, actual transaction prices and estimated values.

3.6 Regional Market Intelligence

This department is especially important.

It should analyze the surrounding regional trading environment and help players understand what visiting merchants and factions are likely to purchase from the colony.

The objective is to answer:

“What goods are merchants in my region most interested in buying from me?”

Do not confuse what a merchant sells with what that merchant is willing to purchase.

Merchant purchasing analysis

For each relevant trader, collect and analyze information such as:

* Trader identity.
* Trader type.
* Associated faction.
* Available purchasing capacity, where exposed by the game.
* Goods the trader is eligible to purchase.
* Goods the trader cannot purchase.
* Historical purchases from the colony.
* Historical sales to the colony.
* Previous trader visits.
* Previous transaction values.
* Relevant trader-specific purchasing restrictions.

Use actual RimWorld trading APIs and definitions to determine purchase eligibility.

Never assume that a trader will purchase an item simply because that item belongs to a broad category.

Regional demand forecasting

Analyze available trader information and historical data to estimate which goods are most promising to sell.

Examples:

* Textiles.
* Weapons.
* Apparel.
* Drugs.
* Food.
* Raw materials.
* Manufactured goods.
* Art.
* Animals, where applicable.

These are examples, not assumptions about universal merchant purchasing eligibility.

Demand intelligence

Provide:

* Most promising goods to sell.
* Goods frequently purchased by relevant traders.
* Historical purchasing trends.
* Merchant-specific demand profiles.
* Estimated profitability.
* Potential trade opportunities.
* Demand rankings.
* Confidence levels for forecasts.

Merchant profiles

Allow players to inspect individual traders and view:

* Their purchasing eligibility.
* Their historical purchases.
* Their previous visits.
* Their purchasing preferences, where supported by evidence.
* Relevant historical transaction information.
* Estimated future demand.

Forecast reliability

This feature must be implemented honestly.

Separate information into three categories:

1. Confirmed: Directly established by the game’s current trading rules or available trader data.
2. Historical: Based on recorded visits and transactions.
3. Estimated: Predicted using available data, trends and trader characteristics.

Never present an estimate as confirmed information.

If there is insufficient historical information, clearly indicate low confidence.

Do not invent hidden merchant preferences or pretend the game exposes purchasing intentions when it does not.

The forecasting system should explain why it recommends a particular good and what evidence supports that recommendation.

3.7 Predictive Intelligence

This department acts as the colony-wide analytical and warning system.

It should combine information from all six other departments.

Its purpose is to identify potential problems and opportunities before they become serious.

Examples:

* Food reserves are projected to run low.
* Power consumption is approaching generation capacity.
* Battery reserves may become insufficient.
* A production chain is likely to stop because of ingredient shortages.
* Storage capacity is approaching its limit.
* A skilled workforce is becoming a bottleneck.
* Valuable resources may be available for profitable trading.
* Colony wealth is declining.
* Production output is insufficient to meet expected demand.

Every prediction should contain:

* Warning or opportunity title.
* Severity.
* Explanation.
* Supporting evidence.
* Affected department.
* Estimated timeframe.
* Confidence level.
* Suggested action.

Use clear severity levels:

* Informational.
* Low.
* Moderate.
* High.
* Critical.

Avoid generating excessive or repetitive warnings.

Predictions must be traceable to actual collected data and should be recalculated when relevant conditions change.

4. Unified Intelligence Dashboard

All seven departments must operate as one integrated system.

The Overview dashboard should aggregate their most important information.

Examples:

* A power shortage prediction should link to Power Intelligence.
* A production bottleneck should link to Production Intelligence.
* A storage issue should link to Logistics Intelligence.
* A workforce shortage should link to Population Intelligence.
* An economic opportunity should link to Economic Intelligence.
* A merchant demand opportunity should link to Regional Market Intelligence.

The Predictive Intelligence department should consume data from all other departments rather than implementing unrelated duplicate analytics.

The interface should support navigation from a high-level warning to the underlying evidence.

5. Analytics and Data Collection Architecture

Create a centralized data collection and analytics system.

Do not implement each department as an isolated collection of hardcoded calculations.

The architecture should provide shared data models, collection services, analytics and forecasting.

Suggested modules:

* Core: Mod initialization, lifecycle and shared services.
* Data: Collection of colony and game information.
* Analytics: Historical analysis, calculations and aggregation.
* Forecasting: Predictions, confidence and warning generation.
* Trading: Trader information, purchasing eligibility and market history.
* UI: Dashboard, departments, charts and interaction.
* Persistence: Historical records and saved configuration.
* Compatibility: RimWorld version and optional framework compatibility.

These are architectural responsibilities, not a requirement to create unnecessary projects or assemblies.

Keep the implementation maintainable and appropriately modular.

6. Historical Data and Persistence

ColonyOS must remember relevant information over time.

Without historical information, many of the analytics and forecasts will have limited usefulness.

Persist appropriate information such as:

* Power generation and consumption history.
* Production trends.
* Resource quantities.
* Colony wealth.
* Population and workforce summaries.
* Trader visits.
* Trade transactions.
* Merchant purchasing history.
* Forecast history where useful.
* User configuration and preferences.

Requirements:

* Save data through RimWorld’s supported save/load mechanisms.
* Preserve compatibility with existing colonies.
* Handle missing or outdated historical data safely.
* Avoid corrupting saves.
* Support new games and existing saves.
* Provide configurable historical data retention.
* Prevent unbounded save-file growth.
* Handle game version differences safely.

Historical information should remain available after restarting the game.

7. Performance Requirements

ColonyOS must not noticeably affect RimWorld performance.

RimWorld colonies can contain many pawns, buildings, resources and objects. Avoid expensive calculations during frequent game updates.

Requirements:

* Do not perform full-colony analytics every tick.
* Use scheduled and event-driven collection wherever practical.
* Cache expensive calculations.
* Update data only when relevant conditions change or on appropriate intervals.
* Separate data collection from rendering.
* Never perform heavy analytics directly during UI drawing.
* Limit historical chart data points.
* Use efficient data structures.
* Avoid unnecessary allocations.
* Avoid repeatedly scanning the entire map.
* Handle large colonies gracefully.
* Make update intervals configurable where appropriate.

The interface should remain responsive even when the colony contains substantial amounts of data.

8. RimWorld Compatibility and Mod Integration

Primary target: RimWorld 1.6.

Secondary target: RimWorld 1.7.

Requirements:

* Support both versions where technically possible.
* Inspect the actual game assemblies and APIs for each target version.
* Do not invent methods, classes, properties or Harmony patches.
* Verify all game API calls against the actual assemblies.
* Isolate version-specific code where necessary.
* Use Harmony only when a suitable API or event is unavailable.
* Keep patches minimal and narrowly scoped.
* Avoid conflicts with common RimWorld mods.
* Handle missing optional dependencies gracefully.
* Do not require any DLC.
* Do not make DLC content necessary for core functionality.

The mod must work in a Core-only installation.

External UI frameworks are allowed as documented dependencies, but the intelligence and analytics systems must not depend on them.

9. User Experience and Configuration

Provide configuration options for:

* Dashboard theme.
* Accent colour.
* Chart appearance.
* Historical data retention.
* Analytics update intervals.
* Forecast update intervals.
* Warning thresholds.
* Notification preferences.
* Enabled or disabled dashboard components.
* Department-specific display preferences.

Avoid overwhelming players with configuration options before they have explored the mod.

Provide sensible defaults.

The interface should be understandable to players who are not familiar with analytics software while still offering depth for advanced users.

10. Development and Implementation Rules

Before writing substantial code:

1. Inspect the actual RimWorld 1.6 and 1.7 assemblies.
2. Research the APIs required by each intelligence department.
3. Evaluate the UI framework candidates.
4. Verify their actual capabilities and compatibility.
5. Design the data models and architecture.
6. Identify which metrics are directly measurable, historically recordable or only estimable.
7. Create a realistic implementation plan.

Do not build the entire system as one enormous class.

Do not create placeholder metrics that display fake numbers.

Do not use static mockups as substitutes for functional UI components.

Do not mark a feature as complete simply because its interface has been created.

If an intended metric cannot be collected reliably, document the limitation and implement the closest honest alternative.

Keep code readable, modular and maintainable.

11. Development Milestones

Implement ColonyOS in stages.

Milestone 1 — Research and Architecture

* Inspect RimWorld 1.6 and 1.7 APIs.
* Evaluate UI frameworks.
* Confirm framework compatibility and licensing.
* Establish the architecture.
* Define shared data models.
* Create the mod structure.
* Verify basic initialization and save compatibility.

Milestone 2 — UI Foundation

* Implement the main ColonyOS interface.
* Create the navigation sidebar.
* Implement the dashboard layout system.
* Establish the design system and themes.
* Implement reusable cards and panels.
* Integrate the selected UI framework.
* Create functional chart components.
* Test layout scaling and interaction inside RimWorld.

The UI foundation must already demonstrate the intended modern dashboard aesthetic.

Milestone 3 — Core Analytics

Implement:

* Power Intelligence.
* Production Intelligence.
* Logistics Intelligence.
* Population Intelligence.

Add data collection, historical records and relevant visualizations.

Milestone 4 — Economic and Trading Systems

Implement:

* Economic Intelligence.
* Regional Market Intelligence.
* Trader profiles.
* Purchasing eligibility analysis.
* Historical trade records.
* Regional demand rankings.
* Merchant demand forecasting.

Milestone 5 — Predictive Intelligence

Implement:

* Cross-department analysis.
* Shortage forecasts.
* Production warnings.
* Power warnings.
* Workforce warnings.
* Storage warnings.
* Economic opportunities.
* Trade recommendations.
* Confidence and evidence reporting.

Milestone 6 — UI Polish and Integration

* Complete the Overview dashboard.
* Refine department-specific layouts.
* Improve chart interactions.
* Add filters and time ranges.
* Add tooltips and drill-down navigation.
* Polish animations and transitions.
* Optimize rendering and large datasets.
* Verify consistent visual design.

Milestone 7 — Testing and Release Preparation

Test:

* RimWorld 1.6.
* RimWorld 1.7.
* Core-only installation.
* New colonies.
* Existing saves.
* Save/load cycles.
* Large colonies.
* UI scaling.
* Framework compatibility.
* Missing optional dependencies.
* Performance during normal gameplay.
* Accuracy of trading eligibility and forecasts.

Capture actual in-game screenshots during development to evaluate the visual quality.

Do not mark a milestone complete until its functionality has been implemented and tested.

12. Final Acceptance Criteria

ColonyOS is considered successful only when all of the following are true:

Functionality

* All seven intelligence departments work.
* The departments share a centralized analytics system.
* Historical information persists.
* Forecasts use real collected data.
* Regional Market Intelligence distinguishes confirmed purchasing eligibility from historical and estimated demand.
* Recommendations include evidence and confidence.
* The system does not automatically take control of colony decisions.

Visual quality

* The interface looks like a modern web-based analytics dashboard.
* It is substantially different from vanilla RimWorld windows.
* The Overview page is visually rich and informative.
* Charts and visualizations are functional.
* Departments have distinct layouts.
* The interface is not a repetitive collection of vertically stacked panels.
* Navigation and drill-down interactions work.
* Themes and visual styling are consistent.
* The interface remains readable at different UI scales.

Technical quality

* RimWorld 1.6 is the primary supported version.
* RimWorld 1.7 is supported where technically possible.
* No DLC is required.
* External UI dependencies are correctly declared.
* The analytics engine is independent of the presentation framework.
* Save compatibility is preserved.
* Performance remains acceptable in large colonies.
* No fake metrics or misleading forecasts are displayed.

13. Final Product Principle

ColonyOS should feel like opening a sophisticated colony management operating system inside RimWorld.

The player should be able to open it and immediately understand the state of their colony through numbers, graphs, visual indicators, trends and actionable intelligence.

It should combine the analytical depth of a management simulation tool with the visual polish of a modern enterprise dashboard.

Do not compromise the visual experience by defaulting to vanilla RimWorld UI patterns. Do not compromise analytical accuracy to make the interface look impressive. Both are equally important to the identity of ColonyOS.
