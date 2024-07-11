import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { OrganizationChartModule } from 'primeng/organizationchart';
import { TableModule } from 'primeng/table';
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { InputTextModule } from 'primeng/inputtext';
import { ButtonModule } from 'primeng/button';
import { AvatarModule } from 'primeng/avatar';
import { DialogModule } from 'primeng/dialog';
import { DropdownModule } from 'primeng/dropdown';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ConfirmPopupModule } from 'primeng/confirmpopup';
import { ToolbarModule } from 'primeng/toolbar';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { FileUploadModule } from 'primeng/fileupload';
import { InputMaskModule } from 'primeng/inputmask';
import { InputSwitchModule } from 'primeng/inputswitch';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextareaModule } from 'primeng/inputtextarea';
import { UtilsModule } from '../../shared/utils/utils.module';
import { CalendarModule } from 'primeng/calendar';
import { CheckboxModule } from 'primeng/checkbox';
import { PaymentsComponent } from './payments.component';
import { PaymentsRoutingModule } from './payments-routing.module';
import { MultiSelectModule } from 'primeng/multiselect';
@NgModule({
  declarations: [PaymentsComponent],
  imports: [
     CommonModule,
    FormsModule,
    ReactiveFormsModule,
    PaymentsRoutingModule,
    OrganizationChartModule,
    TableModule,
    DropdownModule,
    BreadcrumbModule,
    InputTextModule,
    ButtonModule,
    AvatarModule,
    DialogModule,
    AutoCompleteModule,
    ConfirmPopupModule,
    ToolbarModule,
    ToastModule,
    TagModule,
    FileUploadModule,
    InputNumberModule,
    InputMaskModule,
    InputSwitchModule,
    InputTextareaModule,
    UtilsModule,
    CalendarModule,
    CheckboxModule,
    MultiSelectModule
  ]
})
export class PaymentsModule { }
