# Quotation Policy

**Version v1 — 2026-09-18.**

This is the company's own quotation policy: the rules a salesperson would quote from memory when a
customer asks "why this price?". It exists so an explanation can name the rule it rests on instead of
asserting a number.

**How to use this document.** Every figure in a quote has already been computed by QuoteDesk's
pricing code before you see it. This document tells you *which rule produced* a figure, so you can
say "the 200-or-more slab plus tier B" instead of "a discount". It is never a second source of
arithmetic: you never apply a rule below to work out a number, never add two percentages together,
and never check the code's answer against it. If a number you were given appears to disagree with
this document, report the number exactly as given and say that it looks worth checking — do not
correct it.

## Discounts

Two discounts apply to the list price, and they are added together.

**Quantity slab** — by the quantity on the line, per category, inclusive lower bound (a line at
exactly the slab's quantity gets that slab's rate). The standard ladder is:

| Quantity | Discount |
|---|---|
| 1 or more | 0% |
| 50 or more | 3% |
| 200 or more | 6% |
| 500 or more | 9% |

Some categories and individual items carry their own ladder instead of the standard one, so a line's
slab rate is not always the rate this table would suggest. The slab rate you are given for a line is
the one that applies to it.

**Customer tier** — the customer's commercial grade:

| Tier | Discount |
|---|---|
| A | 4% |
| B | 2% |
| C | 0% |

A sender who matches no customer record has no tier and therefore no tier discount.

**Combined cap** — the slab and tier discounts added together never exceed **15%**, however either
one was arrived at. A line whose discount was reduced by this cap is marked as capped.

## Margin floor

A line's net margin after all discounts must stay at or above **10%**. A line below the floor is not
refused: it is marked as requiring an override and routed to a human for approval. Margin figures
themselves are internal and are never stated in a quotation or an explanation — say that a line needs
an override, never by how much it falls short.

## Freight

Flat by destination zone, and waived once the taxable value is **above ₹50,000** — a quote at exactly
₹50,000 still pays freight.

| Zone | Freight | Transit |
|---|---|---|
| Local | ₹0 | 1 day |
| Regional | ₹450 | 3 days |
| National | ₹1,200 | 5 days |

## Tax

**GST at 18%**, applied to the taxable value after all discounts.

## Delivery dates

If the quantity is covered by stock on hand, dispatch is the next working day. Otherwise dispatch is
today plus the supplier's lead time for that item. Delivery is dispatch plus the destination zone's
transit days. Sundays and the company's listed holidays are skipped, so a date that would land on one
rolls forward.

## Quote validity

A quotation is valid for **15 days** from the day it is issued, and the document says so.

## New customers

When the sender matches no customer record: list price with the quantity-slab discount only, no tier
discount and no credit terms, and the quote is flagged as a new-customer enquiry for a human to
verify before it is sent. Quantity economics are real even without a customer record; commercial
grade is not.

## Rounding and currency

Every amount is in Indian Rupees and is rounded to two decimal places, half away from zero. A
percentage is a percentage: write it with `%`, never with `₹`.
