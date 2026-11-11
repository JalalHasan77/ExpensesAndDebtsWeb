<%@ Page Language="VB" AutoEventWireup="false" CodeFile="ExpensesAndDebtsWShop.aspx.vb" Inherits="ExpensesAndDebtsWShop"   MaintainScrollPositionOnPostback="true" EnableEventValidation = false  %>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link rel="stylesheet" href="styles.css" />

    <title></title>
    <style type="text/css">

        .bigCheckbox{
            width:30px;
            height:30px;

        }


        .diffBorderWidth {
            width: 100%;
            border-left-width:70px;
            border-right-width:20px;
            border-Top-width:10px;
            border-bottom-width:10px;
        }

        .MainContainer {
    width: 100%;
    border-collapse: collapse;
    border-style: solid;
    border-width: 0px;
}


        .nowrap {
            white-space: nowrap;
        }

        .itemCell, .itemCellSelecred, .itemCellSelecredBottmBorder {
            text-align: center;
            vertical-align: middle;
        }

        .itemCell a, .itemCellSelecred a, .itemCellSelecredBottmBorder a {
            display: block;
            text-align: center;
            line-height: 50px;
        }

        .full-width-menu {
            width: 100% !important;
            table-layout: fixed;
            border-collapse: collapse;
        }

        .itemCellSelecredBottmBorder,
        .itemCellSelecredBottmBorder a {
            border-bottom: none !important;
        }
        
        /*.Menu-rounded-corners {
            border: 3px solid darkgray;
            -webkit-border-radius: 8px;
            -moz-border-radius: 8px;
            border-radius: 8px;
            overflow: hidden;
            }*/
                
        .GarbageCell {
    border-collapse: collapse;
    border-style: solid;
    border-width: 0px;
   /* border-left-width: 2px;
    border-bottom-width: 0px;
    border-top-width: 0px;
    border-left-width: 1px;
    border-right-width: 0px;*/
    background-color: white;
}

        *,::after,::before{box-sizing:border-box}

        :root {
            --brand-1: #4f46e5;
            --brand-2: #3b82f6;
            --brand-soft: #eef2ff;
            --ink: #1e293b;
            --muted: #64748b;
            --border: #e2e8f0;
            --bg: #eef1f8;
            --success: #16a34a;
            --danger: #ef4444;
            --radius: 14px;
        }

        body {
            font-family: 'Inter', 'Segoe UI', Arial, Helvetica, sans-serif;
            margin: 0;
            padding: 0;
            color: var(--ink);
        }

        /* ---------- Tab-body content (matches ActionAddEdit form styling) ---------- */
        .tab-body-inner {
            padding: 22px 4px 6px;
        }

        .form-row {
            display: flex;
            flex-wrap: wrap;
            align-items: flex-start;
            gap: 6px 18px;
            margin-bottom: 18px;
        }
        .form-row:last-child { margin-bottom: 0; }

        .form-label {
            width: 170px;
            flex-shrink: 0;
            font-size: 13.5px;
            font-weight: 600;
            color: var(--ink);
            padding-top: 9px;
        }

        .form-control-cell { flex: 1; min-width: 220px; }
        .field-hint { font-size: 12px; color: var(--muted); margin-top: 6px; }

        /* ---------- Inputs ---------- */
        .txt-input, .ddl-input {
            width: 100%;
            max-width: 420px;
            padding: 10px 13px;
            border: 1.5px solid var(--border);
            border-radius: 10px;
            font-size: 14px;
            font-family: inherit;
            color: var(--ink);
            background-color: #f8fafc;
            transition: border-color .15s ease, box-shadow .15s ease, background-color .15s ease;
        }

        .txt-input::placeholder { color: #a0aec0; }

        /* ReadOnly mode (?mode=ReadOnly): fields shown but locked */
        .txt-input[readonly], .ddl-input:disabled {
            background: #f8fafc;
            color: var(--ink, #1e293b);
            cursor: default;
            opacity: 1;
        }
        .txt-input[readonly]:focus { box-shadow: none; }

        .txt-input:focus, .ddl-input:focus {
            outline: none;
            border-color: var(--brand-2);
            background-color: #ffffff;
            box-shadow: 0 0 0 4px rgba(59, 130, 246, 0.14);
        }

        .ddl-input {
            appearance: none;
            -webkit-appearance: none;
            background-image: url("data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='16' height='16' fill='none' stroke='%2364748b' stroke-width='2'><path d='M4 6l4 4 4-4'/></svg>");
            background-repeat: no-repeat;
            background-position: right 12px center;
            padding-right: 34px;
            cursor: pointer;
        }

        /* ---------- Date field with inline calendar toggle ---------- */
        .date-input-group {
            display: flex;
            align-items: center;
            gap: 8px;
            position: relative;
        }

        .date-input-group .txt-input { max-width: 160px; }

        .cal-btn {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            padding: 8px 15px;
            border-radius: 9px;
            background-color: #ffffff;
            border: 1.5px solid var(--border);
            font-size: 13px;
            font-weight: 600;
            color: var(--brand-1);
            cursor: pointer;
            transition: all .15s ease;
        }
        .cal-btn:hover { border-color: var(--brand-2); background-color: var(--brand-soft); }

        .age-display {
            font-size: 13.5px;
            font-weight: 600;
            color: var(--ink);
        }

        .calendar-popup {
            position: absolute;
            top: 44px;
            left: 0;
            z-index: 1000;
            background-color: #ffffff;
            border: 1.5px solid var(--border);
            border-radius: var(--radius);
            box-shadow: 0 14px 28px -10px rgba(30, 41, 59, 0.22);
            padding: 6px;
        }

        /* ---------- Paired fields on one row (e.g. Mobile 1 / Mobile 2) ---------- */
        .form-group-inline {
            display: flex;
            flex-wrap: wrap;
            align-items: flex-start;
            gap: 6px 14px;
            flex: 1;
            min-width: 220px;
        }

        .form-label-sm {
            width: 110px;
            flex-shrink: 0;
            font-size: 13.5px;
            font-weight: 600;
            color: var(--ink);
            padding-top: 9px;
        }

        .form-control-cell-sm { flex: 1; min-width: 120px; }

        /* ---------- Bordered section box with overlapping title (fieldset look) ---------- */
        .section-box {
            position: relative;
            border: 1.5px solid var(--border);
            border-radius: var(--radius);
            padding: 30px 20px 18px;
            margin-top: 14px;
            background-color: #fafbfc;
        }

        .section-title {
            position: absolute;
            top: -12px;
            left: 16px;
            background-color: #ffffff;
            padding: 0 8px;
            font-size: 14px;
            font-weight: 700;
            color: var(--ink);
        }

        /* ---------- Chip-styled label (e.g. "Email") ---------- */
        .chip-label {
            display: inline-flex;
            align-items: center;
            width: auto;
            padding: 8px 16px;
            border: 1.5px solid var(--brand-2);
            border-radius: 9px;
            color: var(--brand-1);
            font-size: 13.5px;
            font-weight: 600;
        }

        /* ---------- Section jump link (e.g. "Contact Details >>") ---------- */
        .section-link {
            display: inline-block;
            font-size: 14px;
            font-weight: 700;
            color: var(--brand-1);
            text-decoration: none;
            margin-top: 6px;
        }
        .section-link:hover { text-decoration: underline; color: var(--brand-2); }

        /* ---------- Buttons ---------- */
        .btn-row {
            display: flex;
            justify-content: flex-end;
            gap: 10px;
            margin: 12px -15px 0 -15px;
            padding: 20px 19px 18px;
            border-top: 1px solid #000000;
        }

        .btn-save, .btn-cancel {
            padding: 11px 26px;
            border-radius: 10px;
            font-size: 14px;
            font-weight: 600;
            font-family: inherit;
            border: none;
            cursor: pointer;
            transition: transform .12s ease, box-shadow .12s ease, background-color .12s ease;
        }

        .btn-save {
            background: linear-gradient(120deg, var(--brand-1), var(--brand-2));
            color: #ffffff;
            box-shadow: 0 10px 20px -8px rgba(79, 70, 229, 0.55);
        }
        .btn-save:hover { transform: translateY(-1px); box-shadow: 0 14px 24px -8px rgba(79, 70, 229, 0.6); }

        .btn-cancel {
            background-color: #f1f5f9;
            color: var(--muted);
            border: 1.5px solid var(--border);
        }
        .btn-cancel:hover { background-color: #e2e8f0; color: var(--ink); }

        .msg-success {
            display: block;
            margin: 0 4px 16px;
            padding: 10px 14px;
            border-radius: 10px;
            background-color: #ecfdf5;
            border: 1px solid #a7f3d0;
            color: #15803d;
            font-size: 13.5px;
            font-weight: 600;
        }

        .msg-success:empty { display: none; }

        @media (max-width: 560px) {
            .form-label { width: 100%; padding-top: 0; }
            .form-row { flex-direction: column; }
        }

        .top-strip {
            display: flex;
            align-items: center;
            justify-content: space-between;
            width: 100%;
            margin: 0;
            border: none;
            background: linear-gradient(120deg, #4f46e5, #3b82f6 75%);
            color: #ffffff;
            font-family: Arial, 'Segoe UI', sans-serif;
            font-size: 18px;
            font-weight: 700;
            padding: 14px 20px;
        }

        .top-strip .who { display: flex; flex-direction: column; line-height: 1.25; }
        .top-strip .when { font-size: 13px; font-weight: 600; opacity: .85; }
        .top-strip .user { font-size: 18px; font-weight: 700; }
        .top-strip .user .unknown { color: #fde68a; }

        .close-btn {
            width: 28px;
            height: 28px;
            padding: 4px;
            border: none;
            border-radius: 7px;
            background: transparent;
            cursor: pointer;
            transition: background .15s ease;
        }

        .close-btn:hover,
        .close-btn:focus-visible {
            background: rgba(255, 255, 255, 0.22);
            outline: none;
        }

        
                .tabs {
  display: flex;
}

.tab {
  flex: 1;
}

.tab input[type="radio"] {
  display: none;
}

.tab label {
  display: block;
  padding: 10px;
  background-color: #f0f0f0;
  cursor: pointer;
}

.tab-content {
  display: none;
  padding: 10px;
}

.tab input[type="radio"]:checked + label {
  background-color: #ccc;
}

.tab input[type="radio"]:checked ~ .tab-content {
  display: block;
}

#menu 
{
 width: 1024px;
 height: 25px;
 margin: 0 auto;
 text-align: right;
 background-color: Red;
}
.nestedTable {
    width: 100%;
    border-collapse: collapse;
    white-space: nowrap;
}

.nestedTable3 {
    width: 100%;
    border-collapse: collapse;
    white-space: nowrap;
}
.nestedTable2 {
    /*cellpadding: 25px;
    cell-spacing: 25px;*/
    /*border-width:unset 60px;*/
    width: 95%;
    /*max-width: 1000px;
    min-width: 900px;*/
    /*margin: 10px;*/
    border-collapse: collapse;
    margin: 5px auto;
}


#menuContainer{float: left;}

/* =====================================================================
   Attachments / Comments tabs (same look as Unit.aspx)
   ===================================================================== */
.tab-note { display: block; color: var(--muted, #64748b); padding: 8px 2px; }

/* Toolbar above a tab's content */
.tab-toolbar {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    margin: 14px 0 12px 0;
}
.tab-toolbar.tab-toolbar-start { justify-content: flex-start; gap: 10px; }
.tab-toolbar-left { display: flex; align-items: center; gap: 10px; }
.tab-toolbar-label { font-size: 13px; font-weight: 600; color: var(--ink, #1e293b); }

.tab-ddl {
    min-width: 220px;
    height: 34px;
    padding: 0 10px;
    border: 1.5px solid #e2e8f0;
    border-radius: 8px;
    background: #ffffff;
    color: var(--ink, #1e293b);
    font: inherit;
    font-size: 13px;
}

/* List | Icons switch */
.view-toggle {
    display: inline-flex;
    border: 1.5px solid #e2e8f0;
    border-radius: 8px;
    overflow: hidden;
    background: #ffffff;
}
.view-btn {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    height: 31px;
    padding: 0 12px;
    color: var(--muted, #64748b);
    font-size: 12px;
    font-weight: 600;
    text-decoration: none;
}
.view-btn + .view-btn { border-left: 1.5px solid #e2e8f0; }
.view-btn svg { width: 15px; height: 15px; fill: none; stroke: currentColor; stroke-width: 2; stroke-linecap: round; }
.view-btn:hover { color: #1a1464; background: #f1f4fb; }
.view-btn.active { background: #1a1464; color: #ffffff; }

/* Round "+" button */
.add-btn {
    width: 34px;
    height: 34px;
    padding: 7px;
    border-radius: 50%;
    background: #1a1464;
    cursor: pointer;
}
.add-btn:hover, .add-btn:focus-visible { background: #2a2390; outline: none; }

/* Category heading */
.doc-category {
    margin: 14px 0 6px 0;
    font-size: 13px;
    font-weight: 700;
    color: #1a1464;
    text-transform: uppercase;
    letter-spacing: .03em;
}

/* List view grid */
.grid-wrap {
    overflow-x: auto;
    border: 1.5px solid var(--border, #e2e8f0);
    border-radius: 9px;
    background: #ffffff;
}
.pay-grid { width: 100%; border-collapse: collapse; font-size: 13px; }
.pay-grid th {
    padding: 10px 12px;
    border-bottom: none;
    background: #1a1464;
    color: #ffffff;
    font-weight: 700;
    text-align: left;
    white-space: nowrap;
}
.pay-grid td {
    padding: 10px 12px;
    border-bottom: 1px solid var(--border, #e2e8f0);
    color: var(--ink, #1e293b);
    text-align: left;
    vertical-align: top;
}
.pay-grid tr:last-child td { border-bottom: none; }
.pay-grid tr:nth-child(even) td { background: #ffffff; }
.pay-grid tr:nth-child(odd) td { background: #f1f4fb; }
.pay-grid .seq { width: 50px; color: var(--muted, #64748b); }
.pay-grid .when { white-space: nowrap; color: var(--muted, #64748b); }

/* File link (list view and comments) */
.file-link {
    color: #1a1464;
    font-weight: 600;
    text-decoration: underline;
    text-underline-offset: 3px;
    cursor: pointer;
}
.file-link:hover { color: #3b82f6; }

/* Icons view: one tile per file */
.file-tiles {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(150px, 1fr));
    gap: 12px;
    margin-bottom: 4px;
}
.file-tile {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
    padding: 14px 10px 12px;
    border: 1.5px solid #e2e8f0;
    border-radius: 10px;
    background: #ffffff;
    text-align: center;
}
.file-tile:hover { border-color: #1a1464; }
a.file-tile { text-decoration: none; cursor: pointer; }
.file-icon {
    position: relative;
    width: 44px;
    height: 54px;
    border-radius: 4px 14px 4px 4px;
    background: #94a3b8;
    color: #ffffff;
    font-size: 10px;
    font-weight: 700;
    line-height: 54px;
    letter-spacing: .03em;
}
.file-icon.pdf { background: #dc2626; }
.file-icon.img { background: #16a34a; }
.file-icon.doc { background: #2563eb; }
.file-icon.xls { background: #15803d; }
.file-tile .name {
    max-width: 100%;
    font-size: 12px;
    font-weight: 600;
    color: var(--ink, #1e293b);
    word-break: break-word;
}
.file-tile .doc { font-size: 11px; color: var(--muted, #64748b); }

/* Comments: one 3-row box per comment */
.comment-list { display: flex; flex-direction: column; gap: 14px; }
.comment-card {
    border: 1.5px solid var(--border, #e2e8f0);
    border-radius: 9px;
    overflow: hidden;
    background: #ffffff;
}
.comment-table { width: 100%; border-collapse: collapse; table-layout: fixed; font-size: 14px; }
.comment-table td {
    padding: 10px 14px;
    border-bottom: 1px solid var(--border, #e2e8f0);
    text-align: left;
    vertical-align: top;
    color: var(--ink, #1e293b);
    white-space: normal;
}
.comment-table tr:last-child td { border-bottom: none; }
.comment-table .comment-head td { background: #1a1464; color: #ffffff; font-size: 13px; }
.comment-head .lbl { font-weight: 700; }
.comment-table .comment-body td { white-space: pre-wrap; word-break: break-word; line-height: 1.5; }
.comment-table .comment-files td { background: #f8fafc; font-size: 13px; word-break: break-word; }
.comment-files .lbl { font-weight: 700; margin-right: 6px; }
.comment-files .none { color: var(--muted, #64748b); }




/* =====================================================================
   ORDER TAB (ported from Form1)
   ===================================================================== */
.order-head { display: flex; align-items: flex-start; gap: 34px; flex-wrap: wrap; }
.head-grid { border-collapse: separate; border-spacing: 4px; }
.head-lbl { background: #dbe9f7; border: 1px solid #9fb6cf; padding: 7px 12px; font-size: 13.5px; font-weight: 600; min-width: 150px; white-space: nowrap; }
.head-grid .txt-input { max-width: 220px; padding: 6px 10px; border-radius: 6px; }
.head-grid .txt-input.num { text-align: center; max-width: 140px; }
.total-box { display: flex; border: 1px solid #334155; min-width: 440px; height: 66px; margin-top: 4px; }
.total-cap { width: 170px; background: #dbe9f7; border-right: 1px solid #334155; display: flex; align-items: center; justify-content: center; font: 700 28px Calibri, Arial, sans-serif; }
.total-val { flex: 1; background: #e00000; color: #fff; display: flex; align-items: center; justify-content: center; font: 700 26px Calibri, Arial, sans-serif; }
.shop-box { min-width: 220px; margin-top: 4px; border: 1px solid #334155; background: #f0f3f8; text-align: center; padding: 6px 12px; }
.shop-cap { color: #64748b; font: 700 13px Calibri, Arial, sans-serif; letter-spacing: .02em; }
.shop-val { font: 700 26px Calibri, Arial, sans-serif; color: #15803d; line-height: 1.2; }
.shop-val .neg { color: #b91c1c; }
.shop-sub { font-size: 12px; color: #64748b; }
.dash-card .val .pos { color: #15803d; }
.dash-card .val .neg { color: #b91c1c; }

.order-btns { display: flex; flex-wrap: wrap; gap: 8px; margin: 18px 0 12px; }
.btn-op { padding: 8px 18px; border: 1.5px solid #cbd5e1; border-radius: 8px; background: #f8fafc; color: #1e293b; font: 600 13.5px inherit; cursor: pointer; }
.btn-op:hover { border-color: #3b82f6; background: #eef2ff; }
.btn-op[disabled], .btn-op.aspNetDisabled { opacity: .45; cursor: default; }
.btn-op.gold { background: #facc15; border-color: #eab308; }

.subtabs { display: flex; gap: 4px; border-bottom: 1px solid #94a3b8; }
.subtab { padding: 8px 20px; border: 1px solid #94a3b8; border-bottom: none; border-radius: 7px 7px 0 0; background: #f1f5f9; color: #1e293b; font-weight: 600; text-decoration: none; font-size: 13.5px; }
.subtab.active { background: #fff; position: relative; top: 1px; }
/* Main tabs: same look as Order | Preview, a little larger */
.maintabs { margin-top: 4px; }
.maintabs .subtab { padding: 10px 26px; font-size: 14.5px; }
.maintabs .subtab:hover { background: #e2e8f0; }
.maintabs .subtab.active:hover { background: #fff; }
.maintabs .subtab.locked, .maintabs .subtab.locked:hover { opacity: .4; cursor: not-allowed; background: #f1f5f9; }

/* Payment tab (ported from Default.aspx): two compact rows */
.pay-head { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; margin-bottom: 12px; }
.pay-title { flex: 1; min-width: 200px; background: #111827; color: #fff; text-align: center; font: 700 17px Calibri, Arial, sans-serif; padding: 8px 10px; }
.pay-head .lbl { font-weight: 700; font-size: 14px; }
.pay-head .ddl-input { max-width: 240px; padding: 7px 34px 7px 12px; }
.pay-box { display: flex; align-items: flex-start; justify-content: center; gap: 24px; flex-wrap: wrap; }
.pay-qr img { width: 200px; height: 200px; border: 1px solid #e2e8f0; display: block; }
.pay-info { min-width: 260px; text-align: left; }
.pay-name { font: 700 26px Arial, sans-serif; color: #111827; }
.pay-balance { font: 700 30px Arial, sans-serif; color: #15803d; margin-top: 4px; line-height: 1.1; }
.pay-balance .neg { color: #b91c1c; }
.pay-cap { font-size: 11px; font-weight: 700; color: #64748b; letter-spacing: .04em; text-transform: uppercase; }
.pay-updated { font: 700 12px Arial, sans-serif; color: #475569; margin-top: 10px; }
.pay-amount { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; margin-top: 22px; }
.pay-amount .lbl { font-weight: 700; font-size: 14px; }
.pay-amount .txt-input { max-width: 130px; text-align: center; padding: 7px 10px; }
.grid-scroll { overflow: auto; border: 1px solid #94a3b8; border-top: none; padding: 10px; background: #fff; }

.order-grid, .pv-grid { border-collapse: collapse; font-size: 13px; }
.order-grid td, .pv-grid td, .pv-grid th { border: 1px solid #cbd5e1; padding: 4px 8px; text-align: center; white-space: nowrap; }
.order-grid td.lock { background: #f0f0f0; border-color: #f0f0f0; text-align: right; font-weight: 700; }
.order-grid tr.titles td.it { background: #ffff00; }
.order-grid tr.colhead td { background: #b0c4de; font-weight: 700; }
.order-grid tr.colhead td.first { text-align: left; }
.order-grid td.mem { text-align: left; min-width: 120px; }
.order-grid td.ro { color: #334155; }
.order-grid td.prof { background: #fff7e6; }
.order-grid .acc-tgl { display: inline-block; width: 18px; height: 18px; line-height: 15px; margin-right: 6px; padding: 0; border: 1px solid #94a3b8; border-radius: 4px;
                       background: #fff; color: #1e293b; font: 700 13px Calibri, Arial, sans-serif; text-align: center; cursor: pointer; vertical-align: middle; }
.order-grid .acc-tgl:hover { background: #e2e8f0; }
.order-grid .acc-tgl-gap { display: inline-block; width: 24px; }
.order-grid tr.acc-head td { background: #eef2f7; font-weight: 700; color: #1e293b; }
.order-grid tr.acc-sub td { background: #f8fafc; color: #475569; }
.order-grid tr.acc-sub td.mem { padding-left: 34px; font-size: 13px; }
.order-grid tr.loan td.mem, .order-grid tr.loan td.ro { color: #c2410c; }
.cell-in.profits { font-weight: 700; }
.cell-in { width: 72px; border: none; background: transparent; text-align: center; font: inherit; padding: 3px 2px; }
.cell-in:focus { outline: 2px solid #3b82f6; background: #fff; }
.cell-in[readonly] { color: #334155; }

.pv-grid th { background: #e2e8f0; font-weight: 700; vertical-align: top; }
.pv-grid td.ex { background: #fffacd; }
.pv-grid td.items { text-align: left; white-space: nowrap; }
.pv-grid td.b { font-weight: 700; }
.pv-grid td.neg { color: #dc2626; }
.pv-grid td.mem { text-align: left; }
.pv-grid tr.auto td { color: #6b7280; }
.pv-ddl { min-width: 140px; padding: 3px 4px; font: inherit; }
/* Split payment: the round + next to Account, the payers list, and its dialog */
.pv-grid td.pv-acc { white-space: nowrap; }
.pv-split { display: inline-block; width: 20px; height: 20px; line-height: 18px; margin-left: 6px; border-radius: 50%; border: 1px solid #1a1464;
            color: #1a1464; background: #fff; font-weight: 700; text-align: center; text-decoration: none; vertical-align: middle; }
.pv-split:hover { background: #1a1464; color: #fff; }
.pv-split.on { background: #1a1464; color: #fff; }
.pv-parts { display: inline-block; text-align: left; font-size: 12px; line-height: 1.5; vertical-align: middle; }
.pv-parts b { font-weight: 700; }
.split-grid { border-collapse: collapse; width: 100%; margin: 6px 0 4px; }
.split-grid th, .split-grid td { padding: 4px 6px; text-align: left; }
.split-grid th { font-size: 13px; color: #475569; border-bottom: 1px solid #cbd5e1; }
.split-grid .txt-input { width: 110px; text-align: right; }
.split-sum { margin: 6px 0 10px; font-size: 14px; }
.split-sum .bad { color: #b91c1c; font-weight: 700; }
.split-sum .good { color: #15803d; font-weight: 700; }

.status-bar { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 8px 12px; margin: 10px 0; border-radius: 6px; font: 700 15px 'Segoe UI', Arial, sans-serif; }
.status-ok { background: #dcf5dc; color: #065f46; }
.status-bad { background: #fadede; color: #991b1b; }
.status-warn { background: #fff5c8; color: #92400e; }
.status-info { background: #e2e8f0; color: #111827; }
.status-done { background: #dcebfa; color: #065f46; }
.btn-process { background: #2e8b57; color: #fff; border: none; padding: 9px 26px; border-radius: 6px; font: 700 14px inherit; cursor: pointer; }

.order-msg { display: block; margin: 8px 0; padding: 9px 13px; border-radius: 8px; font-size: 13.5px; font-weight: 600; white-space: pre-line; }
.order-msg.ok { background: #ecfdf5; border: 1px solid #a7f3d0; color: #15803d; }
.order-msg.err { background: #fef2f2; border: 1px solid #fecaca; color: #b91c1c; }

/* Dialogs */
.modal-back { position: fixed; inset: 0; background: rgba(15, 23, 42, .45); z-index: 2000; display: flex; align-items: center; justify-content: center; }
.modal-box { background: #fff; border-radius: 12px; padding: 20px 22px; min-width: 400px; max-width: 680px; max-height: 85vh; overflow: auto; box-shadow: 0 20px 40px -12px rgba(15, 23, 42, .4); }
.modal-box h3 { margin: 0 0 14px; font-size: 17px; }
.modal-row { display: flex; align-items: center; gap: 10px; margin-bottom: 10px; }
.modal-row .lbl { width: 110px; font-weight: 600; font-size: 13.5px; }
.modal-row .txt-input { max-width: 100%; }
.modal-btns { display: flex; justify-content: flex-end; gap: 8px; margin-top: 16px; }
.modal-note { font-size: 13px; color: #475569; margin-bottom: 10px; white-space: pre-line; }
.warn-text { font-size: 13px; color: #b91c1c; white-space: pre-line; margin-bottom: 8px; }
.orders-list { width: 100%; border-collapse: collapse; font-size: 13.5px; }
.orders-list th { background: #1a1464; color: #fff; padding: 7px 10px; text-align: left; }
.orders-list td { padding: 6px 10px; border-bottom: 1px solid #e2e8f0; }
.orders-list a { color: #1a1464; font-weight: 700; }


/* =====================================================================
   PAGE BANNER (Items tab)
   ===================================================================== */
.dash-banner { background: #111827; color: #fff; text-align: center; font: 700 22px Calibri, Arial, sans-serif; padding: 16px 10px; margin-bottom: 14px; }

/* =====================================================================
   ITEMS (ported from Form1)
   ===================================================================== */
.filter-bar { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; margin-bottom: 14px; }
.filter-bar .lbl { font-weight: 700; font-size: 14px; }
.filter-bar .ddl-input { max-width: 260px; padding: 7px 34px 7px 12px; }
.items-grid { width: 100%; max-width: 720px; border-collapse: collapse; font: 15px Calibri, Arial, sans-serif; }
.items-grid th { background: #e2e8f0; padding: 7px 10px; text-align: left; }
.items-grid td { padding: 6px 10px; border-bottom: 1px solid #e2e8f0; }
.items-grid .r { text-align: right; }
.items-grid .b { font-weight: 700; }
.items-grid tr.total td { font-weight: 700; background: #f0f3f8; }

/* =====================================================================
   ACCOUNTS
   ===================================================================== */
.acc-box { max-width: 720px; border: 1px solid #cbd5e1; border-radius: 8px; background: #f8fafc; padding: 10px 18px 14px 18px; margin: 0 0 16px 0; }
.acc-box h3 { margin: 4px 0 10px 0; font-size: 15px; color: #111827; }
.acc-row { display: flex; align-items: center; gap: 10px; margin: 8px 0; flex-wrap: wrap; }
.acc-row .lbl { display: inline-block; width: 120px; font-weight: 700; }
.acc-row .txt-input, .acc-row .ddl-input { max-width: 260px; }
.acc-note { margin: 2px 0 6px 130px; font-size: 13px; color: #64748b; }
.acc-hint { font-size: 12px; color: #64748b; }
.items-grid tr.loan td { color: #c2410c; }
/* =====================================================================
   MATRICES: who must repay whom, who lent to whom (per person)
   ===================================================================== */
.mx-wrap { display: flex; flex-direction: column; gap: 18px; max-width: 520px; }
fieldset.mx-person { border: 1px solid #cbd5e1; border-radius: 10px; background: #f8fafc; padding: 4px 16px 12px; margin: 0; }
fieldset.mx-person > legend { padding: 0 8px; font-size: 16px; font-weight: 700; color: #1a1464; }
.mx-list { border-collapse: collapse; width: 100%; font: 14px Calibri, Arial, sans-serif; }
.mx-list td { padding: 3px 6px; white-space: nowrap; }
.mx-list td.r { text-align: right; }
.mx-list tr.head td { font-weight: 700; padding-top: 10px; border-bottom: 1px solid #e2e8f0; }
.mx-list tr.head.repay td { color: #b91c1c; }
.mx-list tr.head.lend td { color: #15803d; }
.mx-list tr.part td.who { padding-left: 26px; }
.mx-list td .via { font-size: 12px; color: #94a3b8; margin-left: 6px; }

/* =====================================================================
   MAIN DASHBOARD: people and their accounts' balances
   ===================================================================== */
.db-grid { border-collapse: collapse; font: 14px Calibri, Arial, sans-serif; min-width: 560px; }
.db-grid th { background: #111827; color: #fff; padding: 7px 12px; text-align: left; white-space: nowrap; }
.db-grid th.r, .db-grid td.r { text-align: right; }
.db-grid td { padding: 6px 12px; border-bottom: 1px solid #e2e8f0; white-space: nowrap; }
.db-grid tr.person td { font-weight: 700; background: #eef2f7; }
.db-grid tr.acc td { color: #475569; }
.db-grid tr.acc td.name { padding-left: 34px; }
.db-grid tr.acc.loan td.name { color: #c2410c; }
.db-grid td .type { font-size: 12px; color: #94a3b8; margin-left: 6px; font-weight: 400; }
.db-grid td .note { font-size: 12px; color: #94a3b8; margin-left: 6px; font-weight: 400; }
.db-grid td.neg { color: #dc2626; }
.db-grid tr.total td { font-weight: 700; background: #111827; color: #fff; }
.db-grid tr.total td.neg { color: #fca5a5; }

/* =====================================================================
   TRANSACTIONS: the person, then one grid per account
   ===================================================================== */
.tx-person { border: 1px solid #cbd5e1; border-radius: 10px; background: #f8fafc; padding: 12px 16px; margin: 4px 0 18px; }
.tx-person h2 { margin: 0 0 4px; font-size: 20px; color: #111827; }
.tx-person .info { color: #475569; font-size: 13px; margin-bottom: 10px; }
.tx-person .info span { margin-right: 18px; white-space: nowrap; }
.tx-person .info b { color: #1e293b; }
.tx-cards { display: flex; flex-wrap: wrap; gap: 10px; }
.tx-card { width: 160px; height: 60px; display: flex; overflow: hidden; }
.tx-card .bar { width: 5px; }
.tx-card .body { padding: 6px 10px; }
.tx-card .cap { color: #64748b; font: 700 12px Calibri, Arial, sans-serif; }
.tx-card .val { font: 700 22px Calibri, Arial, sans-serif; margin-top: 2px; }
.tx-card .val.neg { color: #b22222 !important; }
.tx-cards.small .tx-card { width: 130px; height: 46px; }
.tx-cards.small .tx-card .cap { font-size: 11px; }
.tx-cards.small .tx-card .val { font-size: 17px; margin-top: 0; }
fieldset.tx-acc { border: 1px solid #cbd5e1; border-radius: 10px; background: #f8fafc; padding: 6px 16px 14px; margin: 0 0 22px; min-width: 0; }
fieldset.tx-acc > legend { padding: 0 8px; font-size: 16px; font-weight: 700; color: #1a1464; }
fieldset.tx-acc > legend .type { font-size: 12px; font-weight: 400; color: #64748b; margin-left: 6px; }
fieldset.tx-acc.loan > legend { color: #c2410c; }
.tx-acc .tx-cards { margin: 4px 0 10px; }
.tx-acc .tx-grid { background: #fff; }
.tx-scroll { overflow-x: auto; }
.tx-grid { border-collapse: collapse; font: 14px Calibri, Arial, sans-serif; }
.tx-grid th { background: #111827; color: #fff; padding: 7px 9px; white-space: nowrap; text-align: left; }
.tx-grid td { padding: 5px 9px; border-bottom: 1px solid #e2e8f0; vertical-align: top; white-space: nowrap; }
.tx-grid td.r { text-align: right; }
.tx-grid td.ex { background: #fffacd; }
.tx-grid td.b { font-weight: 700; }
.tx-grid td.neg { color: #dc2626; }
.tx-grid td.items { line-height: 1.45; }
.tx-wrap.hide-ex .exc { display: none; }

.acc-grid td.acc-acts { white-space: nowrap; text-align: right; }
.acc-grid .acc-act { color: #1a1464; font-weight: 700; font-size: 13px; text-decoration: none; margin-left: 12px; }
.acc-grid .acc-act:hover { text-decoration: underline; }
.acc-grid .acc-act.del { color: #b91c1c; }
.items-grid td.c { text-align: center; }


        .btn-danger { background: #b91c1c !important; color: #fff !important; border-color: #991b1b !important; }

        /* ---------- Payment sign-in (CPR + one-time code) ---------- */
        .otp-card { max-width: 560px; margin: 16px auto; padding: 18px 22px; border: 1px solid #cbd5e1; border-radius: 8px; background: #f8fafc; }
        .otp-card h3 { margin: 4px 0 8px 0; font-size: 17px; color: #111827; }
        .otp-note { color: #475569; margin-bottom: 12px; }
        .otp-row { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; margin-top: 12px; }
        .otp-cpr { width: 200px; font-size: 16px; letter-spacing: 1px; }
        .otp-boxes { display: flex; gap: 10px; margin: 6px 0 4px 0; }
        .otp-box { width: 46px; height: 54px; text-align: center; font-size: 24px; font-weight: bold; border: 1px solid #94a3b8; border-radius: 6px; background: #fff; }
        .otp-box:focus { outline: none; border-color: #1d4ed8; box-shadow: 0 0 0 3px rgba(29, 78, 216, .2); }
        .pay-signout { margin-left: 14px; font-size: 13px; white-space: nowrap; }
        @media (max-width: 480px) { .otp-box { width: 38px; height: 46px; font-size: 20px; } .otp-boxes { gap: 6px; } }

        /* ---------- Member groups: filter in the Order grid's corner, checkboxes in Add New Member ---------- */
        .order-grid td.grp-cell { text-align: left; vertical-align: middle; padding: 3px 6px; }
        .grp-filter { width: 100%; max-width: 260px; padding: 3px 6px; font-size: 13px; border: 1px solid #94a3b8; border-radius: 4px; background: #fff; }
        .shop-filter { display: block; margin-top: 4px; border-color: #b45309; color: #92400e; }
        .grp-row { align-items: flex-start; }
        .grp-box { display: flex; flex-direction: column; gap: 8px; }
        .grp-checks label { margin: 0 14px 0 4px; }
        .grp-checks input { vertical-align: middle; }
        .grp-new { display: flex; gap: 8px; align-items: center; }
        .grp-new .txt-input { width: 150px; }
        .mm-comment-new { width: 260px; resize: vertical; }

        /* ---------- Members panel: the three lines, and the page sliding in from the right ---------- */
        .menu-btn { width: 40px; height: 40px; padding: 8px; border: none; border-radius: 8px; background: transparent; cursor: pointer; display: flex; align-items: center; justify-content: center; transition: background .15s ease; }
        .menu-btn:hover, .menu-btn:focus-visible { background: rgba(255, 255, 255, .22); outline: none; }
        body.mm-locked { overflow: hidden; }
        .mm-backdrop { position: fixed; inset: 0; background: rgba(15, 23, 42, .45); opacity: 0; pointer-events: none; transition: opacity .25s ease; z-index: 900; }
        .mm-backdrop.open { opacity: 1; pointer-events: auto; }
        .mm-panel { position: fixed; top: 0; right: 0; height: 100vh; width: min(1400px, 94vw); background: #fff; box-shadow: -12px 0 32px rgba(15, 23, 42, .25);
                    transform: translateX(100%); transition: transform .3s ease; z-index: 901; display: flex; flex-direction: column; font-family: Arial, 'Segoe UI', sans-serif; }
        .mm-panel.open { transform: translateX(0); }
        .mm-head { display: flex; align-items: center; gap: 12px; padding: 14px 18px; color: #fff; background: linear-gradient(120deg, #4f46e5, #3b82f6 75%); flex-wrap: wrap; }
        .mm-head h2 { margin: 0 10px 0 0; font-size: 20px; }
        .mm-search { flex: 1 1 260px; min-width: 180px; }
        .mm-count { font-size: 13px; opacity: .9; white-space: nowrap; }
        .mm-close { margin-left: auto; width: 36px; height: 36px; border: none; border-radius: 8px; background: transparent; color: #fff; font-size: 26px; line-height: 1; cursor: pointer; }
        .mm-close:hover, .mm-close:focus-visible { background: rgba(255, 255, 255, .22); outline: none; }
        .mm-body { flex: 1; overflow: auto; padding: 0 0 8px 0; }
        .mm-foot { display: flex; align-items: center; justify-content: flex-end; gap: 12px; padding: 12px 18px; border-top: 1px solid #e2e8f0; background: #f8fafc; }
        .mm-dirty { margin-right: auto; color: #b45309; font-weight: bold; font-size: 13px; }
        .mm-grid { border-collapse: collapse; width: 100%; font-size: 13px; }
        .mm-grid th { position: sticky; top: 0; background: #1e3a8a; color: #fff; text-align: left; padding: 7px 6px; white-space: nowrap; z-index: 1; }
        .mm-grid td { border-bottom: 1px solid #e2e8f0; padding: 4px 6px; vertical-align: top; }
        .mm-grid input[type=text], .mm-grid select, .mm-grid textarea { width: 100%; box-sizing: border-box; padding: 4px 6px; font-size: 13px; border: 1px solid #cbd5e1; border-radius: 4px; background: #fff; font-family: inherit; }
        .mm-grid textarea { min-width: 200px; min-height: 30px; resize: vertical; }
        .mm-grid td.id { color: #64748b; white-space: nowrap; }
        .mm-grid td.num { text-align: right; font-weight: bold; white-space: nowrap; }
        .mm-grid td.num.neg { color: #b91c1c; }
        .mm-grid .mm-groups label { display: inline-block; white-space: nowrap; margin: 0 8px 2px 0; }
        .mm-grid tr.frozen td { background: #f1f5f9; }
        .mm-grid tr.frozen .mm-name { color: #64748b; text-decoration: line-through; }
        .mm-grid tr.dirty td:first-child { box-shadow: inset 4px 0 0 #f59e0b; }
        .mm-grid tr.err td { background: #fee2e2; }
        .mm-grid .mm-type { color: #475569; white-space: nowrap; }
        .mm-tag { display: inline-block; margin-left: 6px; padding: 1px 6px; border-radius: 10px; background: #cbd5e1; color: #334155; font-size: 11px; font-weight: bold; }
        @media (max-width: 700px) { .mm-panel { width: 100vw; } .mm-head h2 { font-size: 17px; } }
        </style>
    <script type="text/javascript">
        function changeHiddenFieldValue() {
            alert(document.getElementById("meow").value);
            //if (meow.value == true) {
            //    alert('true');

            //}
            //else {
            //    alert('false')

            //}

        }

        function hasClass(el, className) {
            return el && el.className && (' ' + el.className + ' ').indexOf(' ' + className + ' ') > -1;
        }

        function toggleCalendar(btn) {
            // Walk up to the containing .date-input-group, then find its .calendar-popup
            var group = btn;
            while (group && group.nodeType === 1 && !hasClass(group, 'date-input-group')) {
                group = group.parentNode;
            }
            if (!group) { return; }

            var candidates = group.getElementsByTagName('*');
            var pnl = null;
            for (var i = 0; i < candidates.length; i++) {
                if (hasClass(candidates[i], 'calendar-popup')) {
                    pnl = candidates[i];
                    break;
                }
            }
            if (!pnl) { return; }

            if (pnl.style.display === 'none' || pnl.style.display === '') {
                pnl.style.display = 'block';
            } else {
                pnl.style.display = 'none';
            }
        }

        // ---------------- Header clock: keeps the date / time current between postbacks ----------------
        function edClock() {
            var el = document.getElementById('hdrClock');
            if (!el) { return; }
            var d = new Date();
            var days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
            var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
            var p = function (n) { return (n < 10 ? '0' : '') + n; };
            el.textContent = days[d.getDay()] + ' ' + p(d.getDate()) + ' ' + months[d.getMonth()] + ' ' + d.getFullYear() +
                             '   ' + p(d.getHours()) + ':' + p(d.getMinutes()) + ':' + p(d.getSeconds());
        }
        window.addEventListener('load', function () { edClock(); setInterval(edClock, 1000); });

        // ---------------- Order tab: live totals while typing (the server recalculates on every postback) ----------------
        // Order grid: [+] lists a member's accounts below him, [-] hides them (remembered in hfAccOpen)
        function edToggleAcc(btn) {
            var id = btn.getAttribute('data-id');
            var grid = document.getElementById('orderGrid');
            if (!grid || !id) { return false; }
            var open = btn.textContent === '+';
            var rows = grid.querySelectorAll('tr.acc-sub');
            for (var i = 0; i < rows.length; i++) {
                if (rows[i].getAttribute('data-acc-of') === id) { rows[i].style.display = open ? '' : 'none'; }
            }
            btn.textContent = open ? '\u2212' : '+';
            btn.title = open ? 'Hide the accounts' : 'Show the accounts';
            var hf = document.getElementById('hfAccOpen');
            if (hf) {
                var ids = hf.value ? hf.value.split('|') : [], at = ids.indexOf(id);
                if (open && at < 0) { ids.push(id); }
                if (!open && at >= 0) { ids.splice(at, 1); }
                hf.value = ids.join('|');
            }
            return false;
        }

        // Add New Member: an Individual has all the fields; a Shop only its name
        function edMemberKind() {
            var ddl = document.getElementById('ddlNewMemberKind');
            if (!ddl) { return; }
            var shop = ddl.value === 'Shop';
            var rows = document.querySelectorAll('.ind-only'), i;
            for (i = 0; i < rows.length; i++) { rows[i].style.display = shop ? 'none' : ''; }
            var notes = document.querySelectorAll('.shop-only');
            for (i = 0; i < notes.length; i++) { notes[i].style.display = shop ? '' : 'none'; }
            var lbl = document.getElementById('newMemberNameLbl');
            if (lbl) { lbl.textContent = shop ? 'Shop name:' : 'Member:'; }
        }
        if (window.addEventListener) { window.addEventListener('load', edMemberKind); }

        // Transactions: hide / show the ex columns (remembered in hfTxEx between postbacks)
        function edApplyTxEx() {
            var wrap = document.getElementById('txWrap'), hf = document.getElementById('hfTxEx'), btn = document.getElementById('btnTxEx');
            if (!wrap) { return; }
            var hidden = hf && hf.value === '1';
            wrap.className = hidden ? 'tx-wrap hide-ex' : 'tx-wrap';
            if (btn) { btn.textContent = hidden ? 'Show ex columns' : 'Hide ex columns'; }
        }
        function edToggleTxEx() {
            var hf = document.getElementById('hfTxEx');
            if (hf) { hf.value = hf.value === '1' ? '0' : '1'; }
            edApplyTxEx();
        }
        if (window.addEventListener) { window.addEventListener('load', edApplyTxEx); }

        // Split payment dialog: what was entered and what is left of the line's total
        function edSplitSum() {
            var dueEl = document.getElementById('splitDue');
            if (!dueEl) { return; }
            var due = edNum(dueEl.getAttribute('data-due'));
            var boxes = document.querySelectorAll('input[data-split]'), given = 0;
            for (var i = 0; i < boxes.length; i++) { given += edNum(boxes[i].value); }
            var left = Math.round((due - given) * 1000) / 1000;
            var g = document.getElementById('splitGiven'), l = document.getElementById('splitLeft');
            if (g) { g.textContent = given.toFixed(3); }
            if (l) { l.textContent = left.toFixed(3); l.className = left === 0 ? 'good' : 'bad'; }
        }
        if (window.addEventListener) { window.addEventListener('load', edSplitSum); }

        // Accounts tab: the _Have / _Spent note shows only for a Loan
        function edAccTypeNote() {
            var ddl = document.getElementById('ddlAccType');
            var isLoan = ddl && ddl.value === 'Loan';
            var ids = ['accLoanNote', 'accLimitRow', 'accSpentRow'];
            for (var i = 0; i < ids.length; i++) {
                var el = document.getElementById(ids[i]);
                if (el) { el.style.display = isLoan ? '' : 'none'; }
            }
            var op = document.getElementById('accOpeningRow');
            if (op) { op.style.display = isLoan ? 'none' : ''; }
        }
        if (window.addEventListener) { window.addEventListener('load', edAccTypeNote); }

        function edNum(v) { var n = parseFloat(String(v || '').replace(',', '.')); return isNaN(n) ? 0 : n; }

        // A person's header row: the totals of their account rows (Credit, Withdraw, each item)
        function edOwnerSums(grid) {
            var heads = grid.querySelectorAll('tr.acc-head');
            for (var h = 0; h < heads.length; h++) {
                var owner = heads[h].getAttribute('data-owner');
                var rows = grid.querySelectorAll('tr.acc-sub');
                var cells = heads[h].querySelectorAll('td[data-sum]');
                for (var c = 0; c < cells.length; c++) {
                    var key = cells[c].getAttribute('data-sum'), sel;
                    if (key === 'c') { sel = 'input[data-f="c"]'; }
                    else if (key === 'w') { sel = 'input[data-kind="w"]'; }
                    else { sel = 'input[data-col="' + key.substring(1) + '"]'; }
                    var sum = 0;
                    for (var r = 0; r < rows.length; r++) {
                        if (rows[r].getAttribute('data-acc-of') !== owner) { continue; }
                        var box = rows[r].querySelector(sel);
                        if (box) { sum += edNum(box.value); }
                    }
                    if (key === 'c' || key === 'w') { cells[c].textContent = sum !== 0 ? sum.toFixed(3) : ''; }
                    else { cells[c].textContent = sum !== 0 ? String(parseFloat(sum.toFixed(3))) : ''; }
                }
            }
        }

        function edRecalc() {
            var grid = document.getElementById('orderGrid');
            var total = 0;
            if (grid) {
                // Profits item: each member's quantity = sum of (qty x the item's Profit value in the Profit row);
                // no profit on the shop's own rows (Shop, Shop_Stock, Shop_Profit, Shop_Loss)
                var profitOf = {};
                var pins = grid.querySelectorAll('input[data-profit]');
                for (var pi = 0; pi < pins.length; pi++) { profitOf[pins[pi].getAttribute('data-profit')] = edNum(pins[pi].value); }
                var pcells = grid.querySelectorAll('input[data-profits]');
                for (var pc = 0; pc < pcells.length; pc++) {
                    var tr = pcells[pc].closest('tr');
                    var sum = 0;
                    if (tr && tr.getAttribute('data-shop') !== '1') {
                        var rq = tr.querySelectorAll('input[data-col]');
                        for (var r = 0; r < rq.length; r++) {
                            var k = rq[r].getAttribute('data-col');
                            if (profitOf.hasOwnProperty(k)) { sum += edNum(rq[r].value) * profitOf[k]; }
                        }
                    }
                    pcells[pc].value = sum !== 0 ? sum.toFixed(3) : '';
                }

                var subs = grid.querySelectorAll('td[data-sub]');
                for (var s = 0; s < subs.length; s++) {
                    var col = subs[s].getAttribute('data-sub');
                    var price = edNum(subs[s].getAttribute('data-price'));
                    var cnt = 0;
                    var qtys = grid.querySelectorAll('input[data-col="' + col + '"]');
                    for (var q = 0; q < qtys.length; q++) { cnt += edNum(qtys[q].value); }
                    subs[s].textContent = (cnt * price).toFixed(3);
                    var cntCell = document.getElementById('cnt_' + col);
                    if (cntCell) { cntCell.textContent = parseFloat(cnt.toFixed(3)); }
                    total += cnt * price;
                }
                var w = grid.querySelectorAll('input[data-kind="w"]');
                for (var i = 0; i < w.length; i++) { total += edNum(w[i].value); }
                edOwnerSums(grid);
            }
            var d = document.querySelector('[data-role="delivery"]');
            var v = document.querySelector('[data-role="vat"]');
            total += edNum(d ? d.value : 0) + edNum(v ? v.value : 0);
            var t = document.querySelector('[data-role="total"]');
            if (t) { t.textContent = total.toFixed(3); }

            // The Preview no longer matches: hide Process until Calculate is clicked again
            var bar = document.querySelector('[data-role="status"]');
            if (bar && bar.getAttribute('data-kind') !== 'info' && bar.getAttribute('data-kind') !== '') {
                bar.className = 'status-bar status-warn';
                bar.setAttribute('data-kind', 'warn');
                var txt = bar.querySelector('[data-role="status-text"]');
                if (txt) { txt.textContent = '\u26A0  Order changed \u2013 click Calculate again to check'; }
                var btn = document.querySelector('[data-role="process"]');
                if (btn) { btn.style.display = 'none'; }
            }
        }

        document.addEventListener('input', function (e) {
            if (e.target && e.target.getAttribute && e.target.getAttribute('data-calc') !== null) { edRecalc(); }
        });

        // Add New Item: Enter in Title jumps to Price (whose 0.000 is then selected)
        function edTitleKey(e, priceId) {
            if (e.key === 'Enter') {
                e.preventDefault();
                var p = document.getElementById(priceId);
                if (p) { p.focus(); p.select(); }
                return false;
            }
            return true;
        }


        // ---------------- Payment sign-in: six one-digit boxes for the one-time code ----------------
        function edOtpInit() {
            var wrap = document.getElementById('otpBoxes');
            var hidden = document.getElementById('hfPayOtp');
            if (!wrap || !hidden) { return; }
            var boxes = wrap.querySelectorAll('input.otp-box');
            var sync = function () {
                var v = '';
                for (var i = 0; i < boxes.length; i++) { v += boxes[i].value; }
                hidden.value = v;
                return v;
            };
            var fill = function (from, digits) {
                for (var i = 0; i < digits.length && from + i < boxes.length; i++) { boxes[from + i].value = digits.charAt(i); }
                var next = Math.min(from + digits.length, boxes.length - 1);
                boxes[next].focus();
                if (sync().length === boxes.length) {
                    var btn = document.querySelector('[data-role="otp-verify"]');
                    if (btn) { btn.click(); }   // all six digits: verify straight away
                }
            };
            for (var i = 0; i < boxes.length; i++) {
                (function (idx) {
                    var b = boxes[idx];
                    b.addEventListener('input', function () {
                        var digits = b.value.replace(/\D/g, '');
                        b.value = '';
                        if (digits.length) { fill(idx, digits); } else { sync(); }
                    });
                    b.addEventListener('keydown', function (e) {
                        if (e.key === 'Backspace' && !b.value && idx > 0) { boxes[idx - 1].value = ''; boxes[idx - 1].focus(); sync(); e.preventDefault(); }
                        if (e.key === 'ArrowLeft' && idx > 0) { boxes[idx - 1].focus(); }
                        if (e.key === 'ArrowRight' && idx < boxes.length - 1) { boxes[idx + 1].focus(); }
                    });
                    b.addEventListener('paste', function (e) {
                        var t = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '');
                        if (t.length) { e.preventDefault(); fill(idx, t.substring(0, boxes.length - idx)); }
                    });
                    b.addEventListener('focus', function () { b.select(); });
                })(i);
            }
            boxes[0].focus();
        }
        window.addEventListener('load', edOtpInit);

        // ---------------- Members panel (slides in from the right) ----------------
        function edMembersPanel(open) {
            var p = document.getElementById('membersPanel'), b = document.getElementById('membersBackdrop');
            var keep = document.getElementById('hfMembersOpen');
            if (!p) { return; }
            p.classList.toggle('open', open);
            b.classList.toggle('open', open);
            p.setAttribute('aria-hidden', open ? 'false' : 'true');
            document.body.classList.toggle('mm-locked', open);
            if (keep) { keep.value = open ? '1' : ''; }
            if (open) { edMembersFilter(); var s = document.getElementById('mmSearch'); if (s) { setTimeout(function () { s.focus(); }, 250); } }
        }
        function edMembersKeepOpen() { var keep = document.getElementById('hfMembersOpen'); if (keep) { keep.value = '1'; } }
        function edMembersFilter() {
            var q = ((document.getElementById('mmSearch') || {}).value || '').toLowerCase();
            var st = (document.getElementById('mmStatusFilter') || {}).value || '';
            var rows = document.querySelectorAll('#membersPanel tr.mm-row'), shown = 0;
            for (var i = 0; i < rows.length; i++) {
                var r = rows[i], text = r.getAttribute('data-text') || '';
                var inputs = r.querySelectorAll('input[type=text], textarea');
                for (var k = 0; k < inputs.length; k++) { text += ' ' + inputs[k].value.toLowerCase(); }
                var status = (r.querySelector('select.mm-status') || {}).value || '';
                var ok = (!q || text.indexOf(q) >= 0) && (!st || status === st);
                r.style.display = ok ? '' : 'none';
                if (ok) { shown++; }
            }
            var c = document.getElementById('mmCount'); if (c) { c.textContent = shown + ' of ' + rows.length; }
        }
        function edMembersInit() {
            var p = document.getElementById('membersPanel');
            if (!p) { return; }
            var mark = function (e) {
                var r = e.target.closest('tr.mm-row'); if (!r) { return; }
                r.classList.add('dirty');
                if (e.target.classList.contains('mm-status')) { r.classList.toggle('frozen', e.target.value === 'Frozen'); }
                var n = p.querySelectorAll('tr.mm-row.dirty').length;
                document.getElementById('mmDirty').textContent = n ? n + ' member(s) changed - not saved yet' : '';
            };
            p.addEventListener('input', mark);
            p.addEventListener('change', mark);
            document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && p.classList.contains('open')) { edMembersPanel(false); } });
            var keep = document.getElementById('hfMembersOpen');
            if (keep && keep.value === '1') { edMembersPanel(true); }   // after Save, the panel stays open
        }
        window.addEventListener('load', edMembersInit);
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="top-strip">
            <%-- Who is using the page (Windows login -> Members.Member_StaffID -> Member_Name) --%>
            <div class="who">
                <div class="when" id="hdrClock"><asp:Label ID="lblNow" runat="server" /></div>
                <div class="user"><asp:Label ID="lblUser" runat="server" /></div>
            </div>
            <asp:Label ID="lblID" runat="server" Text=""></asp:Label>
            <asp:Label ID="lblMode" runat="server" Text=""></asp:Label>
                        <asp:Label ID="lblIsDialogue" runat="server" Text=""></asp:Label>
            <%-- Members list (administrator): the three lines open a panel that slides in from the right --%>
            <asp:PlaceHolder ID="phMenu" runat="server">
                <button type="button" class="menu-btn" title="Members" aria-label="Members list" aria-controls="membersPanel" onclick="edMembersPanel(true)">
                    <svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="white" stroke-width="2.5" stroke-linecap="round" aria-hidden="true">
                        <line x1="4" y1="7" x2="20" y2="7" /><line x1="4" y1="12" x2="20" y2="12" /><line x1="4" y1="17" x2="20" y2="17" />
                    </svg>
                </button>
            </asp:PlaceHolder>
        </div>

        <asp:PlaceHolder ID="phMembersPanel" runat="server">
            <div class="mm-backdrop" id="membersBackdrop" onclick="edMembersPanel(false)"></div>
            <aside class="mm-panel" id="membersPanel" role="dialog" aria-modal="true" aria-labelledby="mmTitle" aria-hidden="true">
                <div class="mm-head">
                    <h2 id="mmTitle">Members</h2>
                    <input type="search" id="mmSearch" class="txt-input mm-search" placeholder="Search name, Staff ID, mobile, comment..." oninput="edMembersFilter()" />
                    <select id="mmStatusFilter" class="ddl-input" onchange="edMembersFilter()" aria-label="Show">
                        <option value="">All statuses</option>
                        <option value="Active">Active</option>
                        <option value="Frozen">Frozen</option>
                    </select>
                    <span class="mm-count" id="mmCount"></span>
                    <button type="button" class="mm-close" onclick="edMembersPanel(false)" aria-label="Close the members list">&times;</button>
                </div>
                <asp:Label ID="lblMembersMsg" runat="server" CssClass="order-msg ok" Visible="false" />
                <div class="mm-body">
                    <asp:Literal ID="litMembersList" runat="server" />
                </div>
                <div class="mm-foot">
                    <span class="mm-dirty" id="mmDirty"></span>
                    <asp:Button ID="btnMembersSave" runat="server" Text="Save changes" CssClass="btn-save" CausesValidation="false" OnClientClick="edMembersKeepOpen();" />
                    <button type="button" class="btn-cancel" onclick="edMembersPanel(false)">Close</button>
                </div>
            </aside>
            <asp:HiddenField ID="hfMembersOpen" runat="server" ClientIDMode="Static" />
        </asp:PlaceHolder>
        
        <table class="MainContainer"  cellpadding="0px" cellspacing="0px" >
            <tr>
                <td>
                       <div style="padding:10px">
                            <table cellpadding="0" class="nestedTable">
                                <tr>
                                    <td style="border-width: 0;">
                                            <asp:Label ID="lblPackageTitle" runat="server" Font-Names="Arial" Font-Size="20pt" ForeColor="Black" Text="Expenses &amp; Debt" Font-Bold="True"></asp:Label>
                                        </td>
                                    </tr>
                            </table>
                        </div>
                
                
                </td>
            </tr>
            <tr>
                <td>
                    <div style="padding:10px">
<table cellpadding="0" class="nestedTable">
                                <tr>
                                    <td style="border-width: 0;">
                                        <%-- Main tabs: same look as the Order | Preview tabs --%>
                                        <div class="subtabs maintabs">
                                            <asp:LinkButton ID="lnkTab0" runat="server" CssClass="subtab" CommandArgument="0" OnCommand="MainTab_Command" CausesValidation="false">Order</asp:LinkButton>
                                            <asp:LinkButton ID="lnkTab1" runat="server" CssClass="subtab" CommandArgument="1" OnCommand="MainTab_Command" CausesValidation="false">Main Dashboard</asp:LinkButton>
                                            <asp:LinkButton ID="lnkTab2" runat="server" CssClass="subtab" CommandArgument="2" OnCommand="MainTab_Command" CausesValidation="false">Matrices</asp:LinkButton>
                                            <asp:LinkButton ID="lnkTab3" runat="server" CssClass="subtab" CommandArgument="3" OnCommand="MainTab_Command" CausesValidation="false">Items</asp:LinkButton>
                                            <asp:LinkButton ID="lnkTab6" runat="server" CssClass="subtab" CommandArgument="6" OnCommand="MainTab_Command" CausesValidation="false">Accounts</asp:LinkButton>
                                            <asp:LinkButton ID="lnkTab4" runat="server" CssClass="subtab" CommandArgument="4" OnCommand="MainTab_Command" CausesValidation="false">Transactions</asp:LinkButton>
                                            <asp:LinkButton ID="lnkTab5" runat="server" CssClass="subtab" CommandArgument="5" OnCommand="MainTab_Command" CausesValidation="false">Payment</asp:LinkButton>
                                        </div>




                                    </td>
                                </tr>
                                <!-- -->
                                <tr>
                                    <td>
                                        <table style="width:100%" cellpadding="0"  cellspacing="0">
                                            <tr>
                                                <td class="menuClass" style="background-color:white; padding: 0 15px 15px 15px; border: 1px solid #94a3b8; border-top: none;">
                                                    <asp:MultiView ID="MultiView3" runat="server" ActiveViewIndex="0">
                                                        <%-- ---------- Order ---------- --%>
                                                        <asp:View ID="viewOrder" runat="server">
                                                            <div class="tab-body-inner">

                                                                <%-- ===== Order header + TOTAL ===== --%>
                                                                <div class="order-head">
                                                                    <table class="head-grid">
                                                                        <tr>
                                                                            <td class="head-lbl">Order Source</td>
                                                                            <td><asp:TextBox ID="txtOrderSource" runat="server" CssClass="txt-input" /></td>
                                                                            <td style="width:30px"></td>
                                                                            <td class="head-lbl">Delivery Charge</td>
                                                                            <td><asp:TextBox ID="txtDelivery" runat="server" CssClass="txt-input num" TextMode="Number" step="0.001" min="0" /></td>
                                                                        </tr>
                                                                        <tr>
                                                                            <td class="head-lbl">Order Date</td>
                                                                            <td><asp:TextBox ID="txtOrderDate" runat="server" CssClass="txt-input" TextMode="Date" /></td>
                                                                            <td></td>
                                                                            <td class="head-lbl">VAT</td>
                                                                            <td><asp:TextBox ID="txtVAT" runat="server" CssClass="txt-input num" TextMode="Number" step="0.001" min="0" /></td>
                                                                        </tr>
                                                                        <tr>
                                                                            <td class="head-lbl">Order No.</td>
                                                                            <td><asp:TextBox ID="txtOrderNo" runat="server" CssClass="txt-input num" /></td>
                                                                            <td colspan="3"></td>
                                                                        </tr>
                                                                    </table>
                                                                    <div class="total-box">
                                                                        <div class="total-cap">TOTAL</div>
                                                                        <div class="total-val"><asp:Label ID="lblTotal" runat="server" Text="0.000" /></div>
                                                                    </div>
                                                                    <%-- Shop result (saved balances): Shop_Profit - Shop_Loss --%>
                                                                    <div class="shop-box">
                                                                        <div class="shop-cap">SHOP PROFIT &minus; LOSS</div>
                                                                        <div class="shop-val"><asp:Label ID="lblShopResult" runat="server" Text="0.000" /></div>
                                                                        <div class="shop-sub"><asp:Label ID="lblShopResultSub" runat="server" /></div>
                                                                    </div>
                                                                </div>

                                                                <%-- ===== Buttons ===== --%>
                                                                <div class="order-btns">
                                                                    <asp:Button ID="btnAddMember" runat="server" Text="Add New Member" CssClass="btn-op" CausesValidation="false" />
                                                                    <asp:Button ID="btnAddItem" runat="server" Text="Add New Item" CssClass="btn-op" CausesValidation="false" />
                                                                    <asp:Button ID="btnCalculate" runat="server" Text="Calculate" CssClass="btn-op" CausesValidation="false" />
                                                                    <asp:Button ID="btnNewOrder" runat="server" Text="New Order" CssClass="btn-op" CausesValidation="false"
                                                                        OnClientClick="return confirm('Start a new order?\nItems, quantities, Credit, Withdraw and the Preview will be cleared.');" />
                                                                    <asp:Button ID="btnLoadPrevious" runat="server" Text="Load Previous" CssClass="btn-op" CausesValidation="false" />
                                                                    <asp:Button ID="btnCreateProfits" runat="server" Text="Create Profit Column" CssClass="btn-op" CausesValidation="false" />
                                                                    <asp:Button ID="btnCancelOrder" runat="server" Text="Cancel Order" CssClass="btn-op btn-danger" CausesValidation="false" Visible="false"
                                                                        OnClientClick="return confirm('Cancel this order?\n\nA reversing journal is posted: every balance goes back to what it was before this order.\nThe order and its cancellation both stay in the history.');" />
                                                                </div>

                                                                <asp:Label ID="lblOrderMsg" runat="server" CssClass="order-msg ok" Visible="false" />

                                                                <%-- ===== Order | Preview ===== --%>
                                                                <div class="subtabs">
                                                                    <asp:LinkButton ID="lnkSubOrder" runat="server" CssClass="subtab active" CausesValidation="false">Order</asp:LinkButton>
                                                                    <asp:LinkButton ID="lnkSubPreview" runat="server" CssClass="subtab" CausesValidation="false">Preview</asp:LinkButton>
                                                                </div>

                                                                <asp:MultiView ID="mvOrder" runat="server" ActiveViewIndex="0">

                                                                    <%-- Order grid: grey block + Subtotal / No. Of Item, yellow item titles, blue column titles, one row per member --%>
                                                                    <asp:View ID="viewOrderGrid" runat="server">
                                                                        <%-- The members whose accounts are listed ([+] / [-]), kept between postbacks --%>
                                                                        <asp:HiddenField ID="hfAccOpen" runat="server" ClientIDMode="Static" />
                                                                        <div class="grid-scroll">
                                                                            <table id="orderGrid" class="order-grid">
                                                                                <asp:Literal ID="litOrderTop" runat="server" />
                                                                                <asp:Repeater ID="rptMembers" runat="server">
                                                                                    <ItemTemplate>
                                                                                        <%-- A person with several accounts: a header row ([+] / [-], totals) above their account rows --%>
                                                                                        <asp:Literal ID="litOwnerHead" runat="server" />
                                                                                        <tr<%# RowAttrs(Container.DataItem) %> data-shop='<%# If(IsShopName(Eval("Name")), "1", "") %>'>
                                                                                            <td class="mem"><asp:Literal ID="litMemName" runat="server" /><asp:HiddenField ID="hfIdx" runat="server" /></td>
                                                                                            <td class="ro"><asp:Literal ID="litExBalance" runat="server" /></td>
                                                                                            <td><asp:TextBox ID="txtCredit" runat="server" CssClass="cell-in" /></td>
                                                                                            <td><asp:TextBox ID="txtWithdraw" runat="server" CssClass="cell-in" /></td>
                                                                                            <asp:Repeater ID="rptQty" runat="server">
                                                                                                <ItemTemplate>
                                                                                                    <td><asp:TextBox ID="txtQty" runat="server" CssClass="cell-in" /></td>
                                                                                                </ItemTemplate>
                                                                                            </asp:Repeater>
                                                                                        </tr>
                                                                                    </ItemTemplate>
                                                                                </asp:Repeater>
                                                                            </table>
                                                                        </div>
                                                                    </asp:View>

                                                                    <%-- Preview --%>
                                                                    <asp:View ID="viewPreview" runat="server">
                                                                        <div class="grid-scroll">
                                                                            <asp:Panel ID="pnlStatus" runat="server" CssClass="status-bar" Visible="false">
                                                                                <asp:Label ID="lblStatus" runat="server" />
                                                                                <asp:Button ID="btnProcess" runat="server" Text="Process" CssClass="btn-process" CausesValidation="false" />
                                                                            </asp:Panel>

                                                                            <table class="pv-grid">
                                                                                <tr>
                                                                                    <th>Member</th><th>exBalance</th><th>exCash</th><th>exExpense</th><th>exReceivable</th><th>exPayable</th>
                                                                                    <th>Credit</th><th>Debt</th><th>OrderItems</th><th>Total</th>
                                                                                    <th>Account<br /><asp:DropDownList ID="ddlHeaderAccount" runat="server" CssClass="pv-ddl" AutoPostBack="true" /></th>
                                                                                    <th>Transaction Type<br /><asp:DropDownList ID="ddlHeaderType" runat="server" CssClass="pv-ddl" AutoPostBack="true" /></th>
                                                                                    <th>Paid From<br /><asp:DropDownList ID="ddlHeaderPaidFrom" runat="server" CssClass="pv-ddl" AutoPostBack="true" /></th>
                                                                                    <th>Next Balance</th><th>Cash</th><th>Expense</th><th>Receivable</th><th>Payable</th>
                                                                                </tr>
                                                                                <asp:Repeater ID="rptPreview" runat="server">
                                                                                    <ItemTemplate>
                                                                                        <tr class='<%# If(CBool(Eval("AutoRow")), "auto", "") %>'>
                                                                                            <td class="mem"><%# Server.HtmlEncode(Convert.ToString(Eval("Name"))) %><asp:HiddenField ID="hfIdx" runat="server" /></td>
                                                                                            <td class="ex"><%# M(Eval("ExBalance")) %></td>
                                                                                            <td class="ex"><%# M(Eval("ExCash")) %></td>
                                                                                            <td class="ex"><%# M(Eval("ExExpense")) %></td>
                                                                                            <td class="ex"><%# M(Eval("ExReceivable")) %></td>
                                                                                            <td class="ex"><%# M(Eval("ExPayable")) %></td>
                                                                                            <td><%# M(Eval("Credit")) %></td>
                                                                                            <td><%# M(Eval("Debt")) %></td>
                                                                                            <td class="items"><%# HtmlLines(Eval("Lines")) %></td>
                                                                                            <td class="b"><%# M(Eval("Total")) %></td>
                                                                                            <td class="pv-acc"><asp:DropDownList ID="ddlAccount" runat="server" CssClass="pv-ddl" AutoPostBack="true" OnSelectedIndexChanged="PreviewSelection_Changed" /><asp:Literal ID="litSplit" runat="server" /><asp:LinkButton ID="lnkSplit" runat="server" CssClass="pv-split" CommandName="Split" CommandArgument='<%# Eval("LineKey") %>' CausesValidation="false" ToolTip="Split the payment between several accounts">+</asp:LinkButton></td>
                                                                                            <td><asp:DropDownList ID="ddlType" runat="server" CssClass="pv-ddl" AutoPostBack="true" OnSelectedIndexChanged="PreviewSelection_Changed" /></td>
                                                                                            <td><asp:DropDownList ID="ddlPaidFrom" runat="server" CssClass="pv-ddl" AutoPostBack="true" OnSelectedIndexChanged="PreviewSelection_Changed" /></td>
                                                                                            <td class='b <%# NegCss(Eval("NextBalance")) %>'><%# M(Eval("NextBalance")) %></td>
                                                                                            <td class="b"><%# M(Eval("Cash")) %></td>
                                                                                            <td class="b"><%# M(Eval("Expense")) %></td>
                                                                                            <td class="b"><%# M(Eval("Receivable")) %></td>
                                                                                            <td class="b"><%# M(Eval("Payable")) %></td>
                                                                                        </tr>
                                                                                    </ItemTemplate>
                                                                                </asp:Repeater>
                                                                            </table>
                                                                            <asp:Label ID="lblNoPreview" runat="server" CssClass="tab-note" Text="Click Calculate to build the Preview." />
                                                                        </div>
                                                                    </asp:View>
                                                                </asp:MultiView>

                                                                <%-- ===== Dialog: Split payment (the round + next to a Preview row's Account) ===== --%>
                                                                <asp:Panel ID="pnlSplit" runat="server" CssClass="modal-back" Visible="false" DefaultButton="btnSplitSave">
                                                                    <div class="modal-box">
                                                                        <h3>Split payment &ndash; <asp:Label ID="lblSplitFor" runat="server" /></h3>
                                                                        <div class="tab-note">Who pays for this line and how much: each account pays its amount, and the member owes each of them that amount. The amounts must add up to the line's total.</div>
                                                                        <table class="split-grid">
                                                                            <tr><th>Account</th><th>Paid From</th><th>Amount</th><th></th></tr>
                                                                            <asp:Repeater ID="rptSplit" runat="server">
                                                                                <ItemTemplate>
                                                                                    <tr>
                                                                                        <td><asp:DropDownList ID="ddlSplitAccount" runat="server" CssClass="pv-ddl" AutoPostBack="true" OnSelectedIndexChanged="SplitAccount_Changed" /></td>
                                                                                        <td><asp:DropDownList ID="ddlSplitFund" runat="server" CssClass="pv-ddl" /></td>
                                                                                        <td><asp:TextBox ID="txtSplitAmount" runat="server" CssClass="txt-input" MaxLength="15" placeholder="0.000" data-split="1" oninput="edSplitSum();" /></td>
                                                                                        <td><asp:LinkButton ID="lnkSplitRemove" runat="server" CssClass="acc-act del" CommandName="Remove" CommandArgument='<%# Container.ItemIndex %>' CausesValidation="false">Remove</asp:LinkButton></td>
                                                                                    </tr>
                                                                                </ItemTemplate>
                                                                            </asp:Repeater>
                                                                        </table>
                                                                        <asp:LinkButton ID="lnkSplitAdd" runat="server" CssClass="acc-act" CausesValidation="false" style="margin-left:0">+ Add payer</asp:LinkButton>
                                                                        <div class="split-sum">Line total: <asp:Literal ID="litSplitDue" runat="server" /> &nbsp;&middot;&nbsp; Entered: <span id="splitGiven"></span> &nbsp;&middot;&nbsp; Left: <span id="splitLeft"></span></div>
                                                                        <asp:Label ID="lblSplitErr" runat="server" CssClass="warn-text" Visible="false" />
                                                                        <div class="modal-btns">
                                                                            <asp:Button ID="btnSplitSave" runat="server" Text="Save" CssClass="btn-save" CausesValidation="false" />
                                                                            <asp:Button ID="btnSplitClear" runat="server" Text="One payer" CssClass="btn-cancel" CausesValidation="false" ToolTip="Remove the split: back to one payer" />
                                                                            <asp:Button ID="btnSplitCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" />
                                                                        </div>
                                                                    </div>
                                                                </asp:Panel>

                                                                <%-- ===== Dialog: Add New Member ===== --%>
                                                                <asp:Panel ID="pnlAddMember" runat="server" CssClass="modal-back" Visible="false">
                                                                    <div class="modal-box">
                                                                        <h3>Add New Member</h3>
                                                                        <div class="modal-row"><span class="lbl">Customer Type:</span>
                                                                            <asp:DropDownList ID="ddlNewMemberKind" runat="server" ClientIDMode="Static" CssClass="ddl-input" onchange="edMemberKind();">
                                                                                <asp:ListItem Text="Individual" Value="Individual" Selected="True" />
                                                                                <asp:ListItem Text="Shop" Value="Shop" />
                                                                            </asp:DropDownList></div>
                                                                        <div class="modal-row"><span class="lbl" id="newMemberNameLbl">Member:</span><asp:TextBox ID="txtNewMemberName" runat="server" CssClass="txt-input" /></div>
                                                                        <div class="modal-note shop-only" id="newShopNote">The shop gets five accounts: <b>&lt;Shop&gt;_Budget</b>, <b>_Cash</b>, <b>_Loss</b>, <b>_Profit</b> and <b>_Stock</b> (no others can be added).</div>
                                                                        <div class="modal-row ind-only"><span class="lbl">Staff ID:</span><asp:TextBox ID="txtNewMemberStaffId" runat="server" CssClass="txt-input" MaxLength="50" placeholder="Windows login, e.g. 2271" /></div>
                                                                        <div class="modal-row ind-only"><span class="lbl">Mobile No:</span><asp:TextBox ID="txtNewMemberMobile" runat="server" CssClass="txt-input" MaxLength="30" TextMode="Phone" placeholder="e.g. 36001234" /></div>
                                                                        <div class="modal-row ind-only"><span class="lbl">CPR:</span><asp:TextBox ID="txtNewMemberCpr" runat="server" CssClass="txt-input" MaxLength="11" inputmode="numeric" placeholder="9 digits" /></div>
                                                                        <div class="modal-row ind-only"><span class="lbl">Email:</span><asp:TextBox ID="txtNewMemberEmail" runat="server" CssClass="txt-input" MaxLength="100" TextMode="Email" placeholder="name@example.com" /></div>
                                                                        <div class="modal-row ind-only"><span class="lbl">Balance:</span><asp:TextBox ID="txtNewMemberBalance" runat="server" CssClass="txt-input" TextMode="Number" step="0.001" onfocus="this.select();" /></div>
                                                                        <div class="modal-row ind-only"><span class="lbl">Status:</span>
                                                                            <asp:DropDownList ID="ddlNewMemberStatus" runat="server" CssClass="ddl-input">
                                                                                <asp:ListItem Text="Active" Value="Active" Selected="True" />
                                                                                <asp:ListItem Text="Frozen (suspended / inactive)" Value="Frozen" />
                                                                            </asp:DropDownList></div>
                                                                        <div class="modal-row grp-row ind-only"><span class="lbl">Comment:</span><asp:TextBox ID="txtNewMemberComment" runat="server" CssClass="txt-input mm-comment-new" TextMode="MultiLine" Rows="2" MaxLength="1000" placeholder="Optional note about this member" /></div>
                                                                        <div class="modal-row grp-row ind-only"><span class="lbl">Groups:</span>
                                                                            <div class="grp-box">
                                                                                <asp:CheckBoxList ID="cblNewMemberGroups" runat="server" CssClass="grp-checks" RepeatLayout="Flow" RepeatDirection="Horizontal" />
                                                                                <div class="grp-new">
                                                                                    <asp:TextBox ID="txtNewGroupName" runat="server" CssClass="txt-input" MaxLength="50" placeholder="New group" />
                                                                                    <asp:Button ID="btnNewGroupAdd" runat="server" Text="Add group" CssClass="btn-op" CausesValidation="false" />
                                                                                </div>
                                                                            </div>
                                                                        </div>
                                                                        <asp:Label ID="lblAddMemberErr" runat="server" CssClass="warn-text" Visible="false" />
                                                                        <div class="modal-btns">
                                                                            <asp:Button ID="btnAddMemberOk" runat="server" Text="OK" CssClass="btn-save" CausesValidation="false" />
                                                                            <asp:Button ID="btnAddMemberCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" />
                                                                        </div>
                                                                    </div>
                                                                </asp:Panel>

                                                                <%-- ===== Dialog: Add New Item ===== --%>
                                                                <asp:Panel ID="pnlAddItem" runat="server" CssClass="modal-back" Visible="false">
                                                                    <div class="modal-box">
                                                                        <h3>Add New Item</h3>
                                                                        <div class="modal-row"><span class="lbl">Title:</span><asp:TextBox ID="txtNewItemTitle" runat="server" CssClass="txt-input" /></div>
                                                                        <div class="modal-row"><span class="lbl">Price:</span><asp:TextBox ID="txtNewItemPrice" runat="server" CssClass="txt-input" TextMode="Number" step="0.001" onfocus="this.select();" /></div>
                                                                        <div class="modal-btns">
                                                                            <asp:Button ID="btnAddItemOk" runat="server" Text="OK" CssClass="btn-save" CausesValidation="false" />
                                                                            <asp:Button ID="btnAddItemCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" />
                                                                        </div>
                                                                    </div>
                                                                </asp:Panel>

                                                                <%-- ===== Dialog: Process Order (title + warnings) ===== --%>
                                                                <asp:Panel ID="pnlProcess" runat="server" CssClass="modal-back" Visible="false">
                                                                    <div class="modal-box">
                                                                        <h3>Process Order</h3>
                                                                        <asp:Label ID="lblProcessInfo" runat="server" CssClass="modal-note" />
                                                                        <asp:Label ID="lblProcessWarn" runat="server" CssClass="warn-text" />
                                                                        <div class="modal-row"><span class="lbl">Order title:</span><asp:TextBox ID="txtOrderTitle" runat="server" CssClass="txt-input" MaxLength="100" /></div>
                                                                        <div class="modal-btns">
                                                                            <asp:Button ID="btnProcessSave" runat="server" Text="Save" CssClass="btn-save" CausesValidation="false" />
                                                                            <asp:Button ID="btnProcessCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" />
                                                                        </div>
                                                                    </div>
                                                                </asp:Panel>

                                                                <%-- ===== Dialog: Load Previous Order ===== --%>
                                                                <asp:Panel ID="pnlLoad" runat="server" CssClass="modal-back" Visible="false">
                                                                    <div class="modal-box" style="min-width:560px">
                                                                        <h3>Load Previous Order</h3>
                                                                        <div class="modal-note">Click an Order ID to open it (read-only). What is on the order screen now will be cleared.</div>
                                                                        <asp:GridView ID="gvOrders" runat="server" AutoGenerateColumns="false" CssClass="orders-list" GridLines="None">
                                                                            <Columns>
                                                                                <asp:TemplateField HeaderText="Order ID">
                                                                                    <ItemTemplate>
                                                                                        <asp:LinkButton ID="lnkOpenOrder" runat="server" CommandName="OpenOrder" CommandArgument='<%# Eval("Order_ID") %>'
                                                                                            Text='<%# Eval("Order_ID") %>' CausesValidation="false" />
                                                                                    </ItemTemplate>
                                                                                </asp:TemplateField>
                                                                                <asp:BoundField DataField="Order_Date" HeaderText="Order Date" />
                                                                                <asp:BoundField DataField="Order_No" HeaderText="Order Time" />
                                                                                <asp:BoundField DataField="Order_Source" HeaderText="Order Source" />
                                                                                <asp:BoundField DataField="Status" HeaderText="Status" />
                                                                            </Columns>
                                                                        </asp:GridView>
                                                                        <div class="modal-btns">
                                                                            <asp:Button ID="btnLoadCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" />
                                                                        </div>
                                                                    </div>
                                                                </asp:Panel>

                                                            </div>
                                                        </asp:View>
                                                        <%-- ---------- Main Dashboard (empty) ---------- --%>
                                                        <asp:View ID="viewDashboard" runat="server">
                                                            <div class="tab-body-inner">
                                                                <div class="filter-bar">
                                                                    <span class="lbl">Group:</span>
                                                                    <asp:DropDownList ID="ddlDashGroup" runat="server" CssClass="ddl-input" AutoPostBack="true" />
                                                                </div>
                                                                <asp:Label ID="lblDashMsg" runat="server" CssClass="order-msg err" Visible="false" />
                                                                <asp:Literal ID="litDashboard" runat="server" />
                                                            </div>
                                                        </asp:View>
                                                        <%-- ---------- Matrices (empty) ---------- --%>
                                                        <asp:View ID="viewMatrices" runat="server">
                                                            <div class="tab-body-inner">
                                                                <div class="filter-bar">
                                                                    <span class="lbl">Member:</span>
                                                                    <asp:DropDownList ID="ddlMxMember" runat="server" CssClass="ddl-input" AutoPostBack="true" />
                                                                </div>
                                                                <asp:Label ID="lblMxMsg" runat="server" CssClass="order-msg err" Visible="false" />
                                                                <asp:Literal ID="litMatrices" runat="server" />
                                                            </div>
                                                        </asp:View>
                                                        <%-- ---------- Items ---------- --%>
                                                        <asp:View ID="viewItems" runat="server">
                                                            <div class="tab-body-inner">
                                                                <div class="dash-banner" style="max-width:720px">ITEMS BOUGHT</div>
                                                                <div class="filter-bar">
                                                                    <span class="lbl">Member:</span>
                                                                    <asp:DropDownList ID="ddlItemsMember" runat="server" CssClass="ddl-input" AutoPostBack="true" />
                                                                    <asp:Button ID="btnItemsRefresh" runat="server" Text="Refresh" CssClass="btn-op" CausesValidation="false" />
                                                                </div>
                                                                <asp:Label ID="lblItemsMsg" runat="server" CssClass="order-msg err" Visible="false" />
                                                                <asp:Literal ID="litItems" runat="server" />
                                                            </div>
                                                        </asp:View>
                                                        <%-- ---------- Transactions (empty) ---------- --%>
                                                        <asp:View ID="viewTransactions" runat="server">
                                                            <div class="tab-body-inner">
                                                                <div class="filter-bar">
                                                                    <span class="lbl">Member:</span>
                                                                    <asp:DropDownList ID="ddlTxMember" runat="server" CssClass="ddl-input" AutoPostBack="true" />
                                                                    <button type="button" id="btnTxEx" class="btn-op" onclick="edToggleTxEx();">Hide ex columns</button>
                                                                </div>
                                                                <asp:HiddenField ID="hfTxEx" runat="server" ClientIDMode="Static" />
                                                                <asp:Label ID="lblTxMsg" runat="server" CssClass="order-msg err" Visible="false" />
                                                                <div id="txWrap" class="tx-wrap">
                                                                    <%-- The person: their details and the total of their accounts --%>
                                                                    <asp:Literal ID="litTxPerson" runat="server" />
                                                                    <%-- One grid per account --%>
                                                                    <asp:Literal ID="litTransactions" runat="server" />
                                                                </div>
                                                            </div>
                                                        </asp:View>
                                                        <%-- ---------- Payment ---------- --%>
                                                        <asp:View ID="viewPayment" runat="server">
                                                            <div class="tab-body-inner">
                                                                <%-- Row 1: member list on the left, then the banner --%>
                                                                <div class="pay-head">
                                                                    <span class="lbl">Member:</span>
                                                                    <asp:DropDownList ID="ddlPayMember" runat="server" CssClass="ddl-input" AutoPostBack="true" />
                                                                    <div class="pay-title">PAYMENT</div>
                                                                    <asp:LinkButton ID="lnkPaySignOut" runat="server" CssClass="pay-signout" CausesValidation="false" Visible="false">Not you? Sign out</asp:LinkButton>
                                                                </div>
                                                                <asp:Label ID="lblPayMsg" runat="server" CssClass="order-msg err" Visible="false" />
                                                                <%-- Visitors without a Staff ID (outside the intranet): CPR, then the one-time code --%>
                                                                <asp:Panel ID="pnlPayVerify" runat="server" CssClass="otp-card" Visible="false">
                                                                    <asp:Label ID="lblPayVerifyMsg" runat="server" CssClass="order-msg ok" Visible="false" />
                                                                    <asp:Panel ID="pnlPayCprStep" runat="server" DefaultButton="btnPaySendOtp">
                                                                        <h3>Sign in to see your balance</h3>
                                                                        <div class="otp-note">Enter your CPR. A one-time code will be sent to the mobile and email registered with it.</div>
                                                                        <div class="otp-row">
                                                                            <asp:TextBox ID="txtPayCpr" runat="server" CssClass="txt-input otp-cpr" MaxLength="11" inputmode="numeric" autocomplete="off" placeholder="CPR (9 digits)" />
                                                                            <asp:Button ID="btnPaySendOtp" runat="server" Text="Send code" CssClass="btn-save" CausesValidation="false" />
                                                                        </div>
                                                                    </asp:Panel>
                                                                    <asp:Panel ID="pnlPayOtpStep" runat="server" DefaultButton="btnPayVerify">
                                                                        <h3>Enter the code</h3>
                                                                        <div class="otp-boxes" id="otpBoxes">
                                                                            <input type="text" class="otp-box" inputmode="numeric" maxlength="1" autocomplete="one-time-code" aria-label="Digit 1" />
                                                                            <input type="text" class="otp-box" inputmode="numeric" maxlength="1" autocomplete="off" aria-label="Digit 2" />
                                                                            <input type="text" class="otp-box" inputmode="numeric" maxlength="1" autocomplete="off" aria-label="Digit 3" />
                                                                            <input type="text" class="otp-box" inputmode="numeric" maxlength="1" autocomplete="off" aria-label="Digit 4" />
                                                                            <input type="text" class="otp-box" inputmode="numeric" maxlength="1" autocomplete="off" aria-label="Digit 5" />
                                                                            <input type="text" class="otp-box" inputmode="numeric" maxlength="1" autocomplete="off" aria-label="Digit 6" />
                                                                        </div>
                                                                        <asp:HiddenField ID="hfPayOtp" runat="server" ClientIDMode="Static" />
                                                                        <div class="otp-row">
                                                                            <asp:Button ID="btnPayVerify" runat="server" Text="Verify" CssClass="btn-save" CausesValidation="false" data-role="otp-verify" />
                                                                            <asp:LinkButton ID="lnkPayResend" runat="server" CausesValidation="false">Send a new code</asp:LinkButton>
                                                                            <asp:LinkButton ID="lnkPayChangeCpr" runat="server" CausesValidation="false">Use another CPR</asp:LinkButton>
                                                                        </div>
                                                                    </asp:Panel>
                                                                </asp:Panel>
                                                                <%-- Row 2: QR on the left, figures and amount on the right --%>
                                                                <asp:Panel ID="pnlPay" runat="server" CssClass="pay-box">
                                                                    <div class="pay-qr"><asp:Image ID="imgPayQr" runat="server" AlternateText="Payment QR code" /></div>
                                                                    <div class="pay-info">
                                                                        <div class="pay-name"><asp:Label ID="lblPayName" runat="server" /></div>
                                                                        <div class="pay-balance"><asp:Label ID="lblPayBalance" runat="server" Text="0.000" /></div>
                                                                        <div class="pay-cap">Balance</div>
                                                                        <div class="pay-updated">Last updated on: <asp:Label ID="lblPayUpdated" runat="server" /></div>
                                                                        <div class="pay-amount">
                                                                            <span class="lbl">Amount to pay:</span>
                                                                            <asp:TextBox ID="txtPayAmount" runat="server" CssClass="txt-input" TextMode="Number" step="0.001" min="0" />
                                                                            <asp:Button ID="btnPayRegenerate" runat="server" Text="Re-Generate QR" CssClass="btn-op" CausesValidation="false" />
                                                                        </div>
                                                                    </div>
                                                                </asp:Panel>
                                                            </div>
                                                        </asp:View>
                                                        <%-- ---------- Accounts (view 6, administrator) ---------- --%>
                                                        <asp:View ID="viewAccounts" runat="server">
                                                            <div class="tab-body-inner">
                                                                <div class="dash-banner" style="max-width:720px">ACCOUNTS</div>
                                                                <div class="filter-bar">
                                                                    <span class="lbl">Member:</span>
                                                                    <asp:DropDownList ID="ddlAccMember" runat="server" CssClass="ddl-input" AutoPostBack="true" />
                                                                    <asp:Button ID="btnAccNew" runat="server" Text="Create New Account" CssClass="btn-op" CausesValidation="false" Enabled="false" />
                                                                </div>
                                                                <asp:Label ID="lblAccMsg" runat="server" CssClass="order-msg err" Visible="false" />

                                                                <%-- New account: shown after Create New Account --%>
                                                                <asp:Panel ID="pnlAccNew" runat="server" CssClass="acc-box" Visible="false" DefaultButton="btnAccSave">
                                                                    <h3>New account &ndash; <asp:Label ID="lblAccNewFor" runat="server" /></h3>
                                                                    <div class="acc-row"><span class="lbl">Account Name:</span><asp:TextBox ID="txtAccName" runat="server" CssClass="txt-input" MaxLength="40" /></div>
                                                                    <div class="acc-row"><span class="lbl">Account Type:</span>
                                                                        <asp:DropDownList ID="ddlAccType" runat="server" ClientIDMode="Static" CssClass="ddl-input" onchange="edAccTypeNote();">
                                                                            <asp:ListItem Text="Capital" Value="Capital" />
                                                                            <asp:ListItem Text="Loan" Value="Loan" />
                                                                        </asp:DropDownList>
                                                                    </div>
                                                                    <div class="acc-row" id="accOpeningRow"><span class="lbl">Opening Balance:</span>
                                                                        <asp:TextBox ID="txtAccOpening" runat="server" CssClass="txt-input" MaxLength="15" placeholder="0.000" />
                                                                        <span class="acc-hint" id="accOpeningHint">positive = money in hand, negative = money owed</span>
                                                                    </div>
                                                                    <div class="acc-note" id="accLoanNote">A Loan creates two Loan accounts: <b>&lt;Account Name&gt;_Have</b> (what is left: Loan Limit &minus; Initial Spent) and <b>&lt;Account Name&gt;_Spent</b> (what was used, owed).
                                                                        _Have + spent always equals the Loan Limit.</div>
                                                                    <div class="acc-row" id="accLimitRow"><span class="lbl">Loan Limit:</span>
                                                                        <asp:TextBox ID="txtAccLimit" runat="server" CssClass="txt-input" MaxLength="15" placeholder="0.000" />
                                                                        <span class="acc-hint">the most that can be used, e.g. the card limit</span>
                                                                    </div>
                                                                    <div class="acc-row" id="accSpentRow"><span class="lbl">Initial Spent:</span>
                                                                        <asp:TextBox ID="txtAccSpent" runat="server" CssClass="txt-input" MaxLength="15" placeholder="0.000" />
                                                                        <span class="acc-hint">already spent / owed: the _Spent account starts at minus this</span>
                                                                    </div>
                                                                    <div class="acc-row"><span class="lbl"></span>
                                                                        <asp:Button ID="btnAccSave" runat="server" Text="Save" CssClass="btn-save" CausesValidation="false" />
                                                                        <asp:Button ID="btnAccCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" />
                                                                    </div>
                                                                </asp:Panel>

                                                                <%-- Rename account: shown after Rename on a row --%>
                                                                <asp:Panel ID="pnlAccRename" runat="server" CssClass="acc-box" Visible="false" DefaultButton="btnAccRenameSave">
                                                                    <h3>Rename account &ndash; <asp:Label ID="lblAccRenameOld" runat="server" /></h3>
                                                                    <div class="acc-row"><span class="lbl">New Name:</span><asp:TextBox ID="txtAccRename" runat="server" CssClass="txt-input" MaxLength="40" /></div>
                                                                    <div class="acc-row"><span class="lbl"></span>
                                                                        <asp:Button ID="btnAccRenameSave" runat="server" Text="Save" CssClass="btn-save" CausesValidation="false" />
                                                                        <asp:Button ID="btnAccRenameCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" />
                                                                    </div>
                                                                </asp:Panel>

                                                                <%-- Set Balance: shown after Set Balance on a row (Capital accounts and _Spent loans) --%>
                                                                <asp:Panel ID="pnlAccBalance" runat="server" CssClass="acc-box" Visible="false" DefaultButton="btnAccBalanceSave">
                                                                    <h3>Set balance &ndash; <asp:Label ID="lblAccBalanceFor" runat="server" /></h3>
                                                                    <div class="acc-row"><span class="lbl"><asp:Label ID="lblAccBalanceWhat" runat="server" />:</span>
                                                                        <asp:TextBox ID="txtAccBalance" runat="server" CssClass="txt-input" MaxLength="15" placeholder="0.000" />
                                                                        <asp:Label ID="lblAccBalanceHint" runat="server" CssClass="acc-hint" />
                                                                    </div>
                                                                    <div class="acc-row"><span class="lbl"></span>
                                                                        <asp:Button ID="btnAccBalanceSave" runat="server" Text="Save" CssClass="btn-save" CausesValidation="false" />
                                                                        <asp:Button ID="btnAccBalanceCancel" runat="server" Text="Cancel" CssClass="btn-cancel" CausesValidation="false" />
                                                                    </div>
                                                                </asp:Panel>

                                                                <asp:Repeater ID="rptAccounts" runat="server">
                                                                    <HeaderTemplate>
                                                                        <table class="items-grid acc-grid"><tr><th>Account</th><th>Type</th><th class="r">Balance</th><th></th></tr>
                                                                    </HeaderTemplate>
                                                                    <ItemTemplate>
                                                                        <tr class='<%# If(CBool(Eval("IsLoan")), "loan", "") %>'>
                                                                            <td><%# Server.HtmlEncode(Convert.ToString(Eval("Name"))) %></td>
                                                                            <td><%# Server.HtmlEncode(Convert.ToString(Eval("TypeText"))) %></td>
                                                                            <td class="r b"><%# Eval("BalanceText") %></td>
                                                                            <td class="acc-acts">
                                                                                <asp:LinkButton ID="lnkAccBalance" runat="server" CssClass="acc-act" CommandName="BalanceAcc" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                                                                    Visible='<%# CBool(Eval("CanSetBalance")) %>' Text='<%# Eval("BalanceAction") %>' />
                                                                                <asp:LinkButton ID="lnkAccRename" runat="server" CssClass="acc-act" CommandName="RenameAcc" CommandArgument='<%# Eval("Id") %>' CausesValidation="false">Rename</asp:LinkButton>
                                                                                <asp:LinkButton ID="lnkAccDelete" runat="server" CssClass="acc-act del" CommandName="DeleteAcc" CommandArgument='<%# Eval("Id") %>' CausesValidation="false"
                                                                                    Visible='<%# CBool(Eval("CanDelete")) %>'
                                                                                    OnClientClick='<%# "return confirm(" & HttpUtility.JavaScriptStringEncode("Delete the account """ & Convert.ToString(Eval("Name")) & """?", True) & ");" %>'>Delete</asp:LinkButton>
                                                                            </td>
                                                                        </tr>
                                                                    </ItemTemplate>
                                                                    <FooterTemplate>
                                                                        </table>
                                                                    </FooterTemplate>
                                                                </asp:Repeater>
                                                                <asp:Literal ID="litAccEmpty" runat="server" />
                                                            </div>
                                                        </asp:View>
                                                    </asp:MultiView>
                                                </td>
                                            </tr>
                                        </table>
                                     </td>
                                </tr>
                            </table>
                        </div>
                </td>
            </tr>
        </table>
        
    </form>
</body>
</html>
