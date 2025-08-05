import { Component, OnInit } from '@angular/core';
import { UserService } from 'src/app/services/user.service';
import { AuthStateService } from 'src/app/services/auth-state.service';
import { UserDetailsWithRole, RoleType } from 'src/app/models/user.interface';

@Component({
  selector: 'app-guidelines',
  templateUrl: './guidelines.component.html'
})
export class GuidelinesComponent implements OnInit {
  guidelines: any[] = [];
  AdminEmail: string = '';
  contactText: string = 'Contact Administrator';

  constructor(
    private userService: UserService,
    private authStateService: AuthStateService
  ) {}

  ngOnInit(): void {
    const currentUser = this.authStateService.getCurrentUser();
    const orgId = currentUser?.organizationId;
    if (currentUser?.role === RoleType.OrganizationAdmin) {
      this.AdminEmail = 'admin@parkihouni.com';
      this.contactText = 'Contact Super Admin';
      this.guidelines = [
        {
          question: "Why can't I change my email address or role?",
          answer: `Your email address and role are managed by the system's Super Admin to maintain security and organizational integrity. This ensures that your access level and contact information remain consistent and verified.`
        },
        {
          question: 'What information can I modify as an Organization Admin?',
          answer: `You can update all your profile information except your email address and role. This includes:
          • First Name
          • Last Name
          • Position
          • Address
          • Date of Birth
          • Password
          • Profile picture`
        },
        {
          question: 'Why is this restriction in place?',
          answer: `Restricting changes to email and role helps maintain:
          • Security and identity verification
          • Proper access control
          • Compliance with organizational policies
          • Prevention of unauthorized privilege escalation`
        },
        {
          question: 'What should I do if my email or role is incorrect?',
          answer: `If you believe your email address or role is incorrect, please contact the Super Admin for assistance. Provide any necessary documentation to support your request.`
        }
      ];
    } else {
      this.guidelines = [
        {
          question: "Why can't I change my name or email address?",
          answer: `Your full name and email address are managed by your organization's administrator to maintain consistency and security across all organizational systems. This ensures that your identity within the organization remains verified and synchronized with other corporate systems like HR and IT.`
        },
        {
          question: 'How can I update my professional information?',
          answer: `To update your professional information such as your full name, email or position, please contact your IT manager or organization administrator. This process helps maintain data accuracy and ensures proper authorization for such changes.`
        },
        {
          question: 'What information can I modify myself?',
          answer: `You have control over your personal preferences and certain profile information, including:
          • Profile picture
          • Password
          • Address
          • Date of birth
          ` /* • Notification preferences */
        },
        {
          question: 'Why is this level of control necessary?',
          answer: `This approach ensures:
          • Data consistency across organizational systems
          • Enhanced security and identity verification
          • Compliance with organizational policies
          • Prevention of unauthorized changes
          • Proper audit trail for important information changes`
        },
        {
          question: 'What should I do if my information is incorrect?',
          answer: `If you notice any discrepancies in your profile information:
          1. Document the incorrect information
          2. Contact your immediate supervisor or team manager
          3. Submit a formal request to your IT administrator
          4. Provide any necessary supporting documentation
          
          Your request will be reviewed and processed according to organizational policies.`
        }
      ];
    }

    if (currentUser?.role !== RoleType.OrganizationAdmin && orgId) {
      this.userService.getOrganizationAdminEmail(orgId).subscribe({
        next: (email) => {
          this.AdminEmail = email;
          this.contactText = 'Contact Administrator';
        },
        error: (err) => {
          console.error('Failed to fetch admin email:', err);
          this.AdminEmail = '';
        }
      });
    }
  }
} 